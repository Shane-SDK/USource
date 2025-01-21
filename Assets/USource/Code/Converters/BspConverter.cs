using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using USource.Formats.Source.VBSP;
using static USource.Formats.Source.VBSP.VBSPStruct;

namespace USource.Converters
{
    public class BspConverter : Converter
    {
        public readonly static VertexAttributeDescriptor[] staticVertexDescriptor = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float16, 2),
        };
        public readonly static VertexAttributeDescriptor[] physVertexDescriptor = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        };

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
            HashSet<int> skyFaces = new();
            HashSet<ushort> skyLeafs = new();
            short skyLeafCluster = 0;

            Dictionary<int, GameObject> bModelMap = new();

            if (importOptions.cullSkybox && skyCamera != null && skyCamera.TryGetTransformedVector3("origin", out Vector3 cameraPosition))
            {
                dleaf_t skyBoxLeaf = bspFile.leafs.FirstOrDefault(e => e.Contains(cameraPosition));
                skyLeafCluster = skyBoxLeaf.cluster;
                for (int i = 0; i < bspFile.leafs.Length; i++)
                {
                    dleaf_t leaf = bspFile.leafs[i];
                    if (leaf.cluster != skyBoxLeaf.cluster) continue;

                    skyLeafs.Add((ushort)i);
                    for (ushort leafIndex = leaf.firstLeafFace; leafIndex < leaf.firstLeafFace + leaf.numLeafFaces; leafIndex++)
                    {
                        int faceIndex = bspFile.leafFaces[leafIndex];
                        if (!skyFaces.Contains(faceIndex))
                            skyFaces.Add(faceIndex);
                    }
                }
            }

            GameObject worldGo = new GameObject(location.SourcePath);
            worldGo.isStatic = true;

            GameObject worldGeometryGO = new GameObject("World Geometry");
            worldGeometryGO.isStatic = true;
            worldGeometryGO.transform.parent = worldGo.transform;

            // Brushes/World geometry
            for (int modelIndex = 0; modelIndex < bspFile.models.Length; modelIndex++)
            {
                dmodel_t model = bspFile.models[modelIndex];

                if (importOptions.splitWorldGeometry)
                {
                    GameObject modelGO = null;

                    int modelFaceStart = model.FirstFace;
                    int modelFacesEnd = model.FirstFace + model.NumFaces;

                    for (int leafIndex = 0; leafIndex < bspFile.leafs.Length; leafIndex++)
                    {
                        // Get all faces that belong to this leaf
                        dleaf_t leaf = bspFile.leafs[leafIndex];

                        if (leaf.cluster == skyLeafCluster && importOptions.cullSkybox) continue;

                        MeshData meshData = new MeshData(bspFile, leafIndex);
                        for (int leafFaceIndex = leaf.firstLeafFace; leafFaceIndex < leaf.firstLeafFace + leaf.numLeafFaces; leafFaceIndex++)
                        {
                            ushort faceIndex = bspFile.leafFaces[leafFaceIndex];

                            if (faceIndex < modelFaceStart || faceIndex >= modelFacesEnd) continue;

                            meshData.AddFace(ref bspFile.faces[faceIndex]);
                        }

                        if (!meshData.HasGeometry) continue;

                        if (modelGO == null)
                        {
                            modelGO = new GameObject($"[{modelIndex}] Model");
                            modelGO.isStatic = true;
                            modelGO.transform.parent = worldGeometryGO.transform;
                            modelGO.transform.position = model.Origin;
                            bModelMap[modelIndex] = modelGO;
                        }

                        GameObject leafGO = new GameObject($"{leafIndex}");
                        leafGO.isStatic = true;
                        leafGO.transform.parent = modelGO.transform;

                        meshData.CreateMesh(leafGO, ctx);
                    }
                }
                else
                {
                    MeshData meshData = new MeshData(bspFile, modelIndex);
                    for (int faceIndex = model.FirstFace; faceIndex < model.FirstFace + model.NumFaces; faceIndex++)
                    {
                        if (skyFaces.Contains(faceIndex)) continue;

                        meshData.AddFace(ref bspFile.faces[faceIndex]);
                    }

                    if (!meshData.HasGeometry) continue;

                    GameObject modelGO = new GameObject($"[{modelIndex}] Model");
                    modelGO.isStatic = true;
                    modelGO.transform.parent = worldGeometryGO.transform;
                    modelGO.transform.position = model.Origin;
                    bModelMap[modelIndex] = modelGO;

                    meshData.CreateMesh(modelGO, ctx);
                }
            }

            // Static props
            GameObject staticPropsGO = new GameObject("Static Props");
            staticPropsGO.isStatic = true;
            staticPropsGO.transform.parent = worldGo.transform;
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

                    instance.name = $"[{i}] {instance.name}";
                    instance.isStatic = true;
                    instance.transform.position = lump.Origin;
                    instance.transform.rotation = Quaternion.Euler(lump.Angles);
                    instance.transform.parent = staticPropsGO.transform;
                }
            }

            // Entities
            for (int entityIndex = 0; entityIndex < bspFile.entities.Count; entityIndex++)
            {
                BspEntity entity = bspFile.entities[entityIndex];

                entity.TryGetTransformedVector3("origin", out Vector3 position);
                entity.TryGetTransformedVector3("angles", out Vector3 angles);
                entity.TryGetValue("classname", out string className);
                if (entity.TryGetFloat("pitch", out float pitch))
                {
                    angles.x = -pitch;
                }

                GameObject CreateEntityGO(bool isStatic = false)
                {
                    GameObject go = new GameObject($"[{entityIndex}] {className}");
                    go.transform.parent = worldGo.transform;
                    go.transform.position = position;
                    go.transform.rotation= Quaternion.Euler(angles);
                    go.isStatic = isStatic;
                    return go;
                }

                if (entity.TryGetValue("model", out string modelValue))
                {
                    if (modelValue.StartsWith('*'))  // Brush model
                    {
                        if (int.TryParse(modelValue.Substring(1, (modelValue.Length - 1)), out int brushModelIndex) && bModelMap.TryGetValue(brushModelIndex, out GameObject modelGO))
                        {
                            modelGO.transform.position = position;
                        }
                    }
                    else if (USource.ResourceManager.GetUnityObject(new Location(modelValue, Location.Type.Source), out GameObject prefab, ctx.ImportMode, true))  // studioprop model
                    {
                        GameObject instance;
#if UNITY_EDITOR
                        instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as UnityEngine.GameObject;
#else
                        instance = GameObject.Instantiate(prefab);
#endif
                        if (instance == null) continue;

                        instance.name = $"[{entityIndex}] {instance.name}";
                        instance.transform.position = position;
                        instance.transform.rotation = Quaternion.Euler(angles);
                        instance.transform.parent = worldGo.transform;
                    }
                }

                if (className.Contains("light"))
                {
                    LightType type = LightType.Point;

                    switch (className)
                    {
                        case "light_directional": case "light_environment":
                            type = LightType.Directional; break;
                        case "light_spot":
                            type = LightType.Spot; break;
                    }

                    Color color = Color.white;
                    float intensity = 2.0f;
                    float range = 10;

                    if (entity.TryGetVector4("_light", out Vector4 lightValues))
                    {
                        color = new Color(lightValues.x / 255, lightValues.y / 255, lightValues.z / 255);
                        range = lightValues.w / 20;
                        intensity = lightValues.w / (type == LightType.Directional ? 200 : 50);
                    }

                    Light light = CreateEntityGO(true).AddComponent<Light>();
                    light.intensity = intensity;
                    light.type = type;
                    light.range = range;
                    light.color = color;
                    light.shadows = type == LightType.Directional ? LightShadows.Soft : LightShadows.Hard;
#if UNITY_EDITOR
                    light.lightmapBakeType = type == LightType.Directional ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
#endif

                    if (entity.TryGetFloat("_cone", out float cone) && 
                        entity.TryGetFloat("_inner_cone", out float innerCone))
                    {
                        light.innerSpotAngle = innerCone * 2.0f;
                        light.spotAngle = cone * 2.0f;
                    }
                }
            }

            return worldGo;
        }
        public static Vector3 EnsureFinite(Vector3 v)
        {
            return new Vector3(float.IsInfinity(v.x) ? 0 : v.x, float.IsInfinity(v.y) ? 0 : v.y, float.IsInfinity(v.z) ? 0 : v.z);
        }
        public static Vector2 EnsureFinite(Vector2 v)
        {
            return new Vector2(float.IsInfinity(v.x) ? 0 : v.x, float.IsInfinity(v.y) ? 0 : v.y);
        }
        [System.Serializable]
        public struct ImportOptions
        {
            public bool cullSkybox;
            public bool splitWorldGeometry;
            public bool setupDependencies;
        }
        public struct WorldVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public half2 uv;
            public half2 uv2;
        }
        public class MeshData
        {
            public MeshData(VBSPFile bsp, long key)
            {
                this.bsp = bsp;
                vertices = new();
                subMeshes = new();
                subMeshMap = new();
                this.key = key;
            }
            public bool HasGeometry => vertices.Count > 0 && subMeshes.Count > 0;
            readonly VBSPFile bsp;
            readonly long key;
            List<WorldVertex> vertices;
            Dictionary<int, List<uint>> subMeshes;
            List<int> subMeshMap = new();
            public void AddFace(ref dface_t face)
            {
                if (face.DispInfo != -1) return;

                texinfo_t textureInfo = bsp.texInfo[face.TexInfo];
                dtexdata_t textureData = bsp.texData[textureInfo.TexData];
                string materialPath = bsp.textureStringData[textureData.NameStringTableID].ToLower();

                if (USource.noRenderMaterials.Contains(materialPath) || USource.noCreateMaterials.Contains(materialPath))  // Don't include sky and other tool textures
                    return;

                if (!subMeshes.TryGetValue(textureData.NameStringTableID, out List<uint> subIndices))  // Ensure submesh/indices exist
                {
                    subMeshMap.Add(textureData.NameStringTableID);
                    subIndices = new();
                    subMeshes[textureData.NameStringTableID] = subIndices;
                }

                for (int index = 1, k = 0; index < face.NumEdges - 1; index++, k += 3)
                {
                    subIndices.Add((uint)vertices.Count);
                    subIndices.Add((uint)(vertices.Count + index));
                    subIndices.Add((uint)(vertices.Count + index + 1));
                }

                Vector3 tS = new Vector3(-textureInfo.TextureVecs[0].y, textureInfo.TextureVecs[0].z, textureInfo.TextureVecs[0].x);
                Vector3 tT = new Vector3(-textureInfo.TextureVecs[1].y, textureInfo.TextureVecs[1].z, textureInfo.TextureVecs[1].x);

                for (int surfEdgeIndex = face.FirstEdge; surfEdgeIndex < face.FirstEdge + face.NumEdges; surfEdgeIndex++)
                {
                    int surfEdge = bsp.surfEdges[surfEdgeIndex];
                    int edgeIndex = surfEdge > 0 ? 0 : 1;

                    WorldVertex vertex = new();
                    vertex.position = EnsureFinite(bsp.vertices[bsp.edges[Mathf.Abs(surfEdge)].V[edgeIndex]]);
                    float TextureUVS = (Vector3.Dot(vertex.position, tS) + textureInfo.TextureVecs[0].w * USource.settings.sourceToUnityScale) / (textureData.View_Width * USource.settings.sourceToUnityScale);
                    float TextureUVT = -(Vector3.Dot(vertex.position, tT) + textureInfo.TextureVecs[1].w * USource.settings.sourceToUnityScale) / (textureData.View_Height * USource.settings.sourceToUnityScale);
                    vertex.uv = new half2(EnsureFinite(new Vector2(TextureUVS, TextureUVT)));

                    vertices.Add(vertex);
                }
            }
            public bool CreateMesh(GameObject targetGameObject, ImportContext ctx)
            {
                if (vertices.Count == 0 || subMeshes.Count == 0) return false;

                Mesh mesh = new Mesh();
                mesh.name = $"Mesh {key}";
                mesh.SetVertexBufferParams(vertices.Count, staticVertexDescriptor);
                mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count, 0);
                mesh.subMeshCount = subMeshes.Count;

                List<Material> materials = new();
                List<SubMeshDescriptor> subMeshDescriptors = new();
                IndexFormat indexFormat = vertices.Count >= (1 << 16) ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.indexFormat = indexFormat;
                IList indices = indexFormat == IndexFormat.UInt32 ? new List<uint>() : new List<ushort>();
                for (int i = 0; i < subMeshMap.Count; i++)
                {
                    int subMeshIndex = subMeshMap[i];
                    // Create submeshdescriptors
                    List<uint> subMeshIndices = subMeshes[subMeshIndex];
                    subMeshDescriptors.Add(new SubMeshDescriptor(indices.Count, subMeshIndices.Count, MeshTopology.Triangles));

                    // Get Unity materials
                    if (!USource.ResourceManager.GetUnityObject(new Location($"materials/{bsp.textureStringData[subMeshIndex]}.vmt", Location.Type.Source), out Material material, ctx.ImportMode, true))
                        material = new Material(Shader.Find("Shader Graphs/Error"));
                    materials.Add(material);

                    // Transfer triangles
                    if (indexFormat == IndexFormat.UInt16)
                    {
                        List<ushort> castList = indices as List<ushort>;
                        foreach (uint index in subMeshIndices)
                            castList.Add((ushort)index);
                    }
                    else
                    {
                        List<uint> castList = indices as List<uint>;
                        castList.AddRange(subMeshIndices);
                    }
                }

                mesh.SetIndexBufferParams(indices.Count, indexFormat);
                if (indexFormat == IndexFormat.UInt16)
                    mesh.SetIndexBufferData(indices as List<ushort>, 0, 0, indices.Count);
                else
                    mesh.SetIndexBufferData(indices as List<uint>, 0, 0, indices.Count);

                mesh.SetSubMeshes(subMeshDescriptors);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
#if UNITY_EDITOR
                UnityEditor.Unwrapping.GenerateSecondaryUVSet(mesh);
#endif
                mesh.UploadMeshData(true);

                targetGameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                targetGameObject.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                targetGameObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;

#if UNITY_EDITOR
                ctx.AssetImportContext.AddObjectToAsset($"mesh {key}", mesh);
#endif
                return true;
            }
        }
    }
}
