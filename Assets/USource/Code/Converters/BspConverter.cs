using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public BspConverter(string sourcePath, Stream stream) : base(sourcePath, stream)
        {
            location = new Location(sourcePath, Location.Type.Source);
            bspFile = VBSPFile.Load(stream, sourcePath);
        }

        public override UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            GameObject worldGo = new GameObject(location.SourcePath);
            worldGo.isStatic = true;

            for (int modelIndex = 0; modelIndex < bspFile.BSP_Models.Length; modelIndex++)
            {
                // Create a mapping of every material used
                Dictionary<int, UnityEngine.Material> materialMap = new();

                List<Vector3> vertices = new();
                List<Vector2> uvs = new();
                //List<Vector2> lightMapUvs = new();
                Dictionary<int, List<int>> subMeshes = new();
                List<int> sourceMap = new();

                dmodel_t model = bspFile.BSP_Models[modelIndex];

                for (int faceIndex = model.FirstFace; faceIndex < model.FirstFace + model.NumFaces; faceIndex++)
                {
                    dface_t face = bspFile.BSP_Faces[faceIndex];
                    if (face.DispInfo != -1) continue;
                    texinfo_t textureInfo = bspFile.BSP_TexInfo[face.TexInfo];
                    dtexdata_t textureData = bspFile.BSP_TexData[textureInfo.TexData];
                    string materialPath = bspFile.BSP_TextureStringData[textureData.NameStringTableID].ToLower();

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
                        int surfEdge = bspFile.BSP_Surfedges[surfEdgeIndex];
                        int edgeIndex = surfEdge > 0 ? 0 : 1;
                        Vector3 vertex = EnsureNonInfinite(bspFile.BSP_Vertices[bspFile.BSP_Edges[Mathf.Abs(surfEdge)].V[edgeIndex]]);
                        vertices.Add(vertex);

                        float TextureUVS = (Vector3.Dot(vertex, tS) + textureInfo.TextureVecs[0].w * USource.settings.sourceToUnityScale) / (textureData.View_Width * USource.settings.sourceToUnityScale);
                        float TextureUVT = -(Vector3.Dot(vertex, tT) + textureInfo.TextureVecs[1].w * USource.settings.sourceToUnityScale) / (textureData.View_Height * USource.settings.sourceToUnityScale);

                        //float LightmapS = (Vector3.Dot(vertex, lS) + (textureInfo.LightmapVecs[0].w + 0.5f - face.LightmapTextureMinsInLuxels[0]) * USource.settings.sourceToUnityScale) / ((face.LightmapTextureSizeInLuxels[0] + 1) * USource.settings.sourceToUnityScale);
                        //float LightmapT = (Vector3.Dot(vertex, lT) + (textureInfo.LightmapVecs[1].w + 0.5f - face.LightmapTextureMinsInLuxels[1]) * USource.settings.sourceToUnityScale) / ((face.LightmapTextureSizeInLuxels[1] + 1) * USource.settings.sourceToUnityScale);

                        uvs.Add(EnsureNonInfinite(new Vector2(TextureUVS, TextureUVT)));
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

                    if (!USource.ResourceManager.GetUnityObject(new Location($"materials/{bspFile.BSP_TextureStringData[subMeshIndex]}.vmt", Location.Type.Source), out Material material, ctx.ImportMode, true))
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

            return worldGo;
        }
        Vector3 EnsureNonInfinite(Vector3 v)
        {
            return new Vector3(float.IsInfinity(v.x) ? 0 : v.x, float.IsInfinity(v.y) ? 0 : v.y, float.IsInfinity(v.z) ? 0 : v.z);
        }
        Vector2 EnsureNonInfinite(Vector2 v)
        {
            return new Vector2(float.IsInfinity(v.x) ? 0 : v.x, float.IsInfinity(v.y) ? 0 : v.y);
        }
    }
}
