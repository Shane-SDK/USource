using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using USource.Formats.Source.VBSP;
using static USource.Formats.Source.VBSP.VBSPStruct;

namespace USource.Converters
{
    public class BspConverter : Converter
    {
        Location location;
        VBSPFile bspFile;
        ImportOptions importOptions;
        public BspConverter(string sourcePath, Stream stream, ImportOptions importOptions) : base(sourcePath, stream)
        {
            location = new Location(sourcePath, Location.Type.Source);
            bspFile = new VBSPFile(stream, sourcePath);
            this.importOptions = importOptions;
        }
        public override UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            BspEntity skyCamera = bspFile.entities.FirstOrDefault(e => e.values.TryGetValue("classname", out string className) && className == "sky_camera");
            HashSet<int> skyLeafFaces = new();
            HashSet<ushort> skyLeafs = new();

            if (importOptions.cullSkybox && skyCamera != null && skyCamera.TryGetTransformedVector3("origin", out Vector3 cameraPosition))
            {
                dleaf_t skyBoxLeaf = bspFile.leafs.FirstOrDefault(e => e.Contains(cameraPosition));
                for (int i = 0; i < bspFile.leafs.Length; i++)
                {
                    dleaf_t leaf = bspFile.leafs[i];
                    if (leaf.cluster != skyBoxLeaf.cluster) continue;

                    skyLeafs.Add((ushort)i);
                    for (ushort leafIndex = leaf.firstLeafFace; leafIndex < leaf.firstLeafFace + leaf.numLeafFaces; leafIndex++)
                    {
                        int faceIndex = bspFile.leafFaces[leafIndex];
                        if (!skyLeafFaces.Contains(faceIndex))
                            skyLeafFaces.Add(faceIndex);
                    }
                }
            }

            GameObject worldGo = new GameObject(location.SourcePath);
            worldGo.isStatic = true;

            // Brushes/World geometry
            for (int modelIndex = 0; modelIndex < bspFile.models.Length; modelIndex++)
            {
                // Create a mapping of every material used
                Dictionary<int, UnityEngine.Material> materialMap = new();

                List<Vector3> vertices = new();
                List<Vector2> uvs = new();
                //List<Vector2> lightMapUvs = new();
                Dictionary<int, List<int>> subMeshes = new();
                List<int> sourceMap = new();

                dmodel_t model = bspFile.models[modelIndex];

                for (int faceIndex = model.FirstFace; faceIndex < model.FirstFace + model.NumFaces; faceIndex++)
                {
                    if (skyLeafFaces.Contains(faceIndex)) continue;

                    dface_t face = bspFile.faces[faceIndex];
                    if (face.DispInfo != -1) continue;
                    texinfo_t textureInfo = bspFile.texInfo[face.TexInfo];
                    dtexdata_t textureData = bspFile.texData[textureInfo.TexData];
                    string materialPath = bspFile.textureStringData[textureData.NameStringTableID].ToLower();

                    if (USource.noRenderMaterials.Contains(materialPath) || USource.noCreateMaterials.Contains(materialPath))  // Don't include sky and other tool textures
                        continue;

                    if (!subMeshes.TryGetValue(textureData.NameStringTableID, out List<int> indices))  // Ensure submesh/indices exist
                    {
                        sourceMap.Add(textureData.NameStringTableID);
                        indices = new();
                        subMeshes[textureData.NameStringTableID] = indices;
                    }
                    
                    for (int index = 1, k = 0; index < face.NumEdges - 1; index++, k += 3)
                    {
                        indices.Add(vertices.Count);
                        indices.Add(vertices.Count + index);
                        indices.Add(vertices.Count + index + 1);
                    }

                    Vector3 tS = new Vector3(-textureInfo.TextureVecs[0].y, textureInfo.TextureVecs[0].z, textureInfo.TextureVecs[0].x);
                    Vector3 tT = new Vector3(-textureInfo.TextureVecs[1].y, textureInfo.TextureVecs[1].z, textureInfo.TextureVecs[1].x);

                    //Vector3 lS = new Vector3(-textureInfo.LightmapVecs[0].y, textureInfo.LightmapVecs[0].z, textureInfo.LightmapVecs[0].x);
                    //Vector3 lT = new Vector3(-textureInfo.LightmapVecs[1].y, textureInfo.LightmapVecs[1].z, textureInfo.LightmapVecs[1].x);

                    for (int surfEdgeIndex = face.FirstEdge; surfEdgeIndex < face.FirstEdge + face.NumEdges; surfEdgeIndex++)
                    {
                        int surfEdge = bspFile.surfEdges[surfEdgeIndex];
                        int edgeIndex = surfEdge > 0 ? 0 : 1;
                        Vector3 vertex = EnsureFinite(bspFile.vertices[bspFile.edges[Mathf.Abs(surfEdge)].V[edgeIndex]]);
                        vertices.Add(vertex);

                        float TextureUVS = (Vector3.Dot(vertex, tS) + textureInfo.TextureVecs[0].w * USource.settings.sourceToUnityScale) / (textureData.View_Width * USource.settings.sourceToUnityScale);
                        float TextureUVT = -(Vector3.Dot(vertex, tT) + textureInfo.TextureVecs[1].w * USource.settings.sourceToUnityScale) / (textureData.View_Height * USource.settings.sourceToUnityScale);

                        //float LightmapS = (Vector3.Dot(vertex, lS) + (textureInfo.LightmapVecs[0].w + 0.5f - face.LightmapTextureMinsInLuxels[0]) * USource.settings.sourceToUnityScale) / ((face.LightmapTextureSizeInLuxels[0] + 1) * USource.settings.sourceToUnityScale);
                        //float LightmapT = (Vector3.Dot(vertex, lT) + (textureInfo.LightmapVecs[1].w + 0.5f - face.LightmapTextureMinsInLuxels[1]) * USource.settings.sourceToUnityScale) / ((face.LightmapTextureSizeInLuxels[1] + 1) * USource.settings.sourceToUnityScale);

                        uvs.Add(EnsureFinite(new Vector2(TextureUVS, TextureUVT)));
                        //lightMapUvs.Add(new Vector2(LightmapS + textureData.NameStringTableID, LightmapT));
                    }
                }

                if (vertices.Count == 0 || subMeshes.Count == 0) continue;

                GameObject modelGO = new GameObject($"Model {modelIndex}");
                modelGO.isStatic = true;
                modelGO.transform.position = model.Origin;
                Mesh mesh = new Mesh();
                mesh.name = $"Mesh {modelIndex}";
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                //mesh.SetUVs(1, lightMapUvs);
                mesh.subMeshCount = subMeshes.Count;

                List<Material> materials = new();
                for (int i = 0; i < sourceMap.Count; i++)
                {
                    int subMeshIndex = sourceMap[i];
                    mesh.SetIndices(subMeshes[subMeshIndex], MeshTopology.Triangles, i);

                    if (!USource.ResourceManager.GetUnityObject(new Location($"materials/{bspFile.textureStringData[subMeshIndex]}.vmt", Location.Type.Source), out Material material, ctx.ImportMode, true))
                        material = new Material(Shader.Find("Shader Graphs/Error"));
                    materials.Add(material);
                }

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
#if UNITY_EDITOR
                UnityEditor.Unwrapping.GenerateSecondaryUVSet(mesh);
#endif
                mesh.UploadMeshData(true);

                modelGO.AddComponent<MeshFilter>().sharedMesh = mesh;
                modelGO.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                modelGO.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
                modelGO.transform.parent = worldGo.transform;

#if UNITY_EDITOR
                ctx.AssetImportContext.AddObjectToAsset($"mesh {modelIndex}", mesh);
#endif
            }

            // Static props
            for (int i = 0; i < bspFile.staticPropLumps.Length; i++)
            {
                if (skyLeafs.Contains(bspFile.staticPropLeafEntries[i])) continue;  // skip props in the skybox
                StaticPropLump_t lump = bspFile.staticPropLumps[i];

                if (USource.ResourceManager.GetUnityObject(new Location(bspFile.staticPropDict[lump.PropType], Location.Type.Source), out GameObject prefab, ctx.ImportMode, true))
                {
                    GameObject instance;
#if UNITY_EDITOR
                    instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as UnityEngine.GameObject;
#else
                    instance = GameObject.Instantiate(prefab);
#endif
                    if (instance == null) continue;

                    instance.isStatic = true;
                    instance.transform.position = lump.Origin;
                    instance.transform.rotation = Quaternion.Euler(lump.Angles);
                    instance.transform.parent = worldGo.transform;
                }
            }

            return worldGo;
        }
        Vector3 EnsureFinite(Vector3 v)
        {
            return new Vector3(float.IsInfinity(v.x) ? 0 : v.x, float.IsInfinity(v.y) ? 0 : v.y, float.IsInfinity(v.z) ? 0 : v.z);
        }
        Vector2 EnsureFinite(Vector2 v)
        {
            return new Vector2(float.IsInfinity(v.x) ? 0 : v.x, float.IsInfinity(v.y) ? 0 : v.y);
        }
        [System.Serializable]
        public struct ImportOptions
        {
            public bool cullSkybox;
        }
    }
}
