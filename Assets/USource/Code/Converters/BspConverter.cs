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
            HashSet<int> skyLeafFaces = new();
            HashSet<ushort> skyLeafs = new();
            short skyLeafCluster = 0;

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
                        if (!skyLeafFaces.Contains(faceIndex))
                            skyLeafFaces.Add(faceIndex);
                    }
                }
            }

            GameObject worldGo = new GameObject(location.SourcePath);
            worldGo.isStatic = true;

            // MERGE THIS CODE AT SOME POINT

            if (importOptions.splitWorldGeometry)
            {
                GameObject leafGO = new GameObject($"World Geometry");
                leafGO.transform.parent = worldGo.transform;
                leafGO.isStatic = true;

                int modelFaceStart = bspFile.models[0].FirstFace;
                int modelFacesEnd = bspFile.models[0].FirstFace + bspFile.models[0].NumFaces;

                for (int leafIndex = 0; leafIndex < bspFile.leafs.Length; leafIndex++)
                {
                    // Get all faces that belong to this leaf
                    dleaf_t leaf = bspFile.leafs[leafIndex];

                    if (leaf.cluster == skyLeafCluster && importOptions.cullSkybox) continue;

                    // Create a mapping of every material used
                    Dictionary<int, UnityEngine.Material> materialMap = new();
                    List<WorldVertex> vertices = new();
                    Dictionary<int, List<uint>> subMeshes = new();
                    List<int> subMeshMap = new();

                    for (int leafFaceIndex = leaf.firstLeafFace; leafFaceIndex < leaf.firstLeafFace + leaf.numLeafFaces; leafFaceIndex++)
                    {
                        ushort leafFace = bspFile.leafFaces[leafFaceIndex];

                        if (leafFace < modelFaceStart || leafFace >=  modelFacesEnd) continue;

                        dface_t face = bspFile.faces[leafFace];

                        if (face.DispInfo != -1) continue;

                        texinfo_t textureInfo = bspFile.texInfo[face.TexInfo];
                        dtexdata_t textureData = bspFile.texData[textureInfo.TexData];
                        string materialPath = bspFile.textureStringData[textureData.NameStringTableID].ToLower();

                        if (USource.noRenderMaterials.Contains(materialPath) || USource.noCreateMaterials.Contains(materialPath))  // Don't include sky and other tool textures
                            continue;

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
                            int surfEdge = bspFile.surfEdges[surfEdgeIndex];
                            int edgeIndex = surfEdge > 0 ? 0 : 1;

                            WorldVertex vertex = new();
                            vertex.position = EnsureFinite(bspFile.vertices[bspFile.edges[Mathf.Abs(surfEdge)].V[edgeIndex]]);
                            float TextureUVS = (Vector3.Dot(vertex.position, tS) + textureInfo.TextureVecs[0].w * USource.settings.sourceToUnityScale) / (textureData.View_Width * USource.settings.sourceToUnityScale);
                            float TextureUVT = -(Vector3.Dot(vertex.position, tT) + textureInfo.TextureVecs[1].w * USource.settings.sourceToUnityScale) / (textureData.View_Height * USource.settings.sourceToUnityScale);
                            vertex.uv = new half2(EnsureFinite(new Vector2(TextureUVS, TextureUVT)));

                            vertices.Add(vertex);
                        }
                    }

                    if (vertices.Count == 0 || subMeshes.Count == 0) continue;

                    GameObject modelGO = new GameObject($"{leafIndex}");
                    modelGO.isStatic = true;
                    //modelGO.transform.position = model.Origin;
                    Mesh mesh = new Mesh();
                    mesh.name = $"Mesh {leafIndex}";
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
                        if (!USource.ResourceManager.GetUnityObject(new Location($"materials/{bspFile.textureStringData[subMeshIndex]}.vmt", Location.Type.Source), out Material material, ctx.ImportMode, true))
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

                    modelGO.AddComponent<MeshFilter>().sharedMesh = mesh;
                    modelGO.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                    modelGO.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
                    modelGO.transform.parent = leafGO.transform;

#if UNITY_EDITOR
                    ctx.AssetImportContext.AddObjectToAsset($"mesh {leafIndex}", mesh);
#endif
                }
            }
            else
            {
                // Brushes/World geometry
                for (int modelIndex = 0; modelIndex < Mathf.Min(1, bspFile.models.Length); modelIndex++)
                {
                    // Create a mapping of every material used
                    Dictionary<int, UnityEngine.Material> materialMap = new();
                    List<WorldVertex> vertices = new();
                    Dictionary<int, List<uint>> subMeshes = new();
                    List<int> subMeshMap = new();

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
                            int surfEdge = bspFile.surfEdges[surfEdgeIndex];
                            int edgeIndex = surfEdge > 0 ? 0 : 1;

                            WorldVertex vertex = new();
                            vertex.position = EnsureFinite(bspFile.vertices[bspFile.edges[Mathf.Abs(surfEdge)].V[edgeIndex]]);
                            float TextureUVS = (Vector3.Dot(vertex.position, tS) + textureInfo.TextureVecs[0].w * USource.settings.sourceToUnityScale) / (textureData.View_Width * USource.settings.sourceToUnityScale);
                            float TextureUVT = -(Vector3.Dot(vertex.position, tT) + textureInfo.TextureVecs[1].w * USource.settings.sourceToUnityScale) / (textureData.View_Height * USource.settings.sourceToUnityScale);
                            vertex.uv = new half2(EnsureFinite(new Vector2(TextureUVS, TextureUVT)));

                            vertices.Add(vertex);
                        }
                    }

                    if (vertices.Count == 0 || subMeshes.Count == 0) continue;

                    GameObject modelGO = new GameObject($"Model {modelIndex}");
                    modelGO.isStatic = true;
                    modelGO.transform.position = model.Origin;
                    Mesh mesh = new Mesh();
                    mesh.name = $"Mesh {modelIndex}";
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
                    if (!USource.ResourceManager.GetUnityObject(new Location($"materials/{bspFile.textureStringData[subMeshIndex]}.vmt", Location.Type.Source), out Material material, ctx.ImportMode, true))
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

                    modelGO.AddComponent<MeshFilter>().sharedMesh = mesh;
                    modelGO.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                    modelGO.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
                    modelGO.transform.parent = worldGo.transform;

#if UNITY_EDITOR
                    ctx.AssetImportContext.AddObjectToAsset($"mesh {modelIndex}", mesh);
#endif
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
            public bool splitWorldGeometry;
        }
        public struct WorldVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public half2 uv;
            public half2 uv2;
        }
    }
}
