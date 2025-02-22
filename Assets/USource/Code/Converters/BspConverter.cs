﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using USource.Formats.BSP;
using USource.Formats.MDL;

namespace USource.Converters
{
    public class BspConverter : IConverter
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
        BSP bspFile;
        ImportOptions importOptions;
        BspEntity skyCamera;
        HashSet<int> skyFaces;
        HashSet<ushort> usedFaces;
        short skyLeafCluster;
        Vector3 skyCameraPosition;
        float skyCameraScale = 1;
        public BspConverter(Stream stream, ImportOptions importOptions)
        {
            bspFile = new BSP(stream);
            this.importOptions = importOptions;
        }
        Vector3 TransformSkyboxToWorld(Vector3 skyPos)
        {
            Vector3 local = skyPos - skyCameraPosition;

            return local * skyCameraScale;
        }
        public UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            skyCamera = bspFile.entities.FirstOrDefault(e => e.values.TryGetValue("classname", out string className) && className == "sky_camera");
            skyFaces = new();
            usedFaces = new();
            skyLeafCluster = short.MinValue;
            skyCameraPosition = Vector3.zero;

            Dictionary<int, GameObject> bModelMap = new();

            if (skyCamera != null && skyCamera.TryGetTransformedVector3("origin", out skyCameraPosition))
            {
                Leaf skyBoxLeaf = bspFile.leafs.FirstOrDefault(e => e.Contains(skyCameraPosition));
                skyLeafCluster = skyBoxLeaf.cluster;
                for (int i = 0; i < bspFile.leafs.Length; i++)
                {
                    Leaf leaf = bspFile.leafs[i];
                    if (leaf.cluster != skyBoxLeaf.cluster) continue;

                    for (ushort leafIndex = leaf.firstLeafFace; leafIndex < leaf.firstLeafFace + leaf.leafFaceCount; leafIndex++)
                    {
                        int faceIndex = bspFile.leafFaces[leafIndex];
                        if (!skyFaces.Contains(faceIndex))
                            skyFaces.Add(faceIndex);
                    }
                }
            }
            skyCamera?.TryGetFloat("scale", out skyCameraScale);

            GameObject worldGo = new GameObject(location.SourcePath);
            worldGo.isStatic = true;

            GameObject worldGeometryGO = new GameObject("World Geometry");
            worldGeometryGO.isStatic = true;
            worldGeometryGO.transform.parent = worldGo.transform;

            // Brushes/World geometry
            for (int modelIndex = 0; modelIndex < bspFile.models.Length; modelIndex++)
            {
                if (!importOptions.objects.HasFlag(ObjectFlags.StaticWorld) && modelIndex == 0) continue;
                if (!importOptions.objects.HasFlag(ObjectFlags.BrushModels) && modelIndex != 0) break;

                BrushModel model = bspFile.models[modelIndex];

                int modelFaceStart = model.firstFace;
                int modelFacesEnd = model.firstFace + model.faceCount;

                GameObject modelGO = new GameObject($"[{modelIndex}] Model");
                modelGO.isStatic = true;
                modelGO.transform.parent = worldGeometryGO.transform;
                modelGO.transform.position = model.origin;
                bModelMap[modelIndex] = modelGO;


                bool splitGeometry = importOptions.splitWorldGeometry && modelIndex == 0;
                if (splitGeometry)  // Use leaf faces
                {
                    for (int i = 0; i < bspFile.dispInfo.Length; i++)
                    {
                        DisplacementInfo dispInfo = bspFile.dispInfo[i];
                        ushort faceIndex = dispInfo.face;
                        if ((skyFaces.Contains(faceIndex) && importOptions.skyboxMode == SkyboxMode.Cull) || usedFaces.Contains(faceIndex)) continue;

                        MeshData meshData = new MeshData(bspFile, i, this);
                        meshData.AddFace(ref bspFile.faces[faceIndex], faceIndex);
                        usedFaces.Add(faceIndex);

                        if (!meshData.HasGeometry) continue;

                        GameObject dispGO = new GameObject($"disp {i}");
                        dispGO.isStatic = true;
                        dispGO.transform.parent = modelGO.transform;

                        meshData.CreateMesh(dispGO, ctx);
                    }

                    for (int leafIndex = 0; leafIndex < bspFile.leafs.Length; leafIndex++)
                    {
                        // Get all faces that belong to this leaf
                        Leaf leaf = bspFile.leafs[leafIndex];

                        if (leaf.cluster == skyLeafCluster && importOptions.skyboxMode == SkyboxMode.Cull) continue;

                        MeshData meshData = new MeshData(bspFile, leafIndex, this);
                        for (int leafFaceIndex = leaf.firstLeafFace; leafFaceIndex < leaf.firstLeafFace + leaf.leafFaceCount; leafFaceIndex++)
                        {
                            ushort faceIndex = bspFile.leafFaces[leafFaceIndex];

                            if (usedFaces.Contains(faceIndex)) continue;

                            meshData.AddFace(ref bspFile.faces[faceIndex], faceIndex);
                            usedFaces.Add(faceIndex);
                        }

                        if (!meshData.HasGeometry) continue;

                        GameObject leafGO = new GameObject($"{leafIndex}");
                        leafGO.isStatic = true;
                        leafGO.transform.parent = modelGO.transform;

                        meshData.CreateMesh(leafGO, ctx);
                    }
                }
                else
                {
                    MeshData meshData = new MeshData(bspFile, modelIndex, this);
                    for (int faceIndex = modelFaceStart; faceIndex < modelFacesEnd; faceIndex++)
                    {
                        if ((skyFaces.Contains(faceIndex) && importOptions.skyboxMode == SkyboxMode.Cull) || usedFaces.Contains((ushort)faceIndex)) continue;

                        meshData.AddFace(ref bspFile.faces[faceIndex], faceIndex);
                        usedFaces.Add((ushort)faceIndex);
                    }

                    if (!meshData.HasGeometry) continue;

                    meshData.CreateMesh(modelGO, ctx);
                }
            }

            // World Collision
            if (importOptions.importWorldColliders)
            {
                foreach (KeyValuePair<int, PhysModel> pair in bspFile.physModels)
                {
                    if (!bModelMap.TryGetValue(pair.Key, out GameObject go)) continue;

                    ModelConverter.CreateColliders(go, pair.Value.solids, ctx, pair.Key != 0);
                }
            }

            // Static props
            if (importOptions.objects.HasFlag( ObjectFlags.Props ))
            {
                GameObject staticPropsGO = new GameObject("Static Props");
                staticPropsGO.isStatic = true;
                staticPropsGO.transform.parent = worldGo.transform;
                for (int i = 0; i < bspFile.staticPropLumps.Length; i++)
                {
                    //bool isSkybox = bspFile.leafs[bspFile.staticPropLeafEntries[i]].cluster == skyLeafCluster;
                    //if (isSkybox && importOptions.skyboxMode == SkyboxMode.Cull) continue;

                    StaticProp lump = bspFile.staticPropLumps[i];

                    if (USource.ResourceManager.GetUnityObject(new Location(bspFile.staticPropDict[lump.propType], Location.Type.Source), out GameObject prefab, ctx.ImportMode, true))
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
                        instance.transform.position = lump.origin;
                        instance.transform.rotation = Quaternion.Euler(lump.angles);
                        instance.transform.parent = staticPropsGO.transform;

                        //if (isSkybox && importOptions.skyboxMode == SkyboxMode.Scale)
                        //{
                        //    instance.transform.position = TransformSkyboxToWorld(lump.origin);
                        //    instance.transform.localScale = Vector3.one * 16.0f;
                        //}
                    }
                }
            }

            // Entities
            for (int entityIndex = 0; entityIndex < bspFile.entities.Count; entityIndex++)
            {
                BspEntity entity = bspFile.entities[entityIndex];

                entity.TryGetValue("classname", out string className);
                if (className == null) continue;
                entity.TryGetVector3("origin", out Vector3 position);
                entity.TryGetVector3("angles", out Vector3 angles);


                position = IConverter.SourceTransformPoint(position);
                angles = IConverter.SourceTransformAngles(angles);

                bool skybox = false;
                if (importOptions.skyboxMode == SkyboxMode.Scale && bspFile.leafs.Any(e => (e.cluster == skyLeafCluster) && (e.Contains(position))))
                {
                    skybox = true;
                    position = TransformSkyboxToWorld(position);
                }

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

                if (importOptions.objects.HasFlag( ObjectFlags.Props) && entity.TryGetValue("model", out string modelValue))
                {
                    if (modelValue.StartsWith('*'))  // Brush model
                    {
                        if (int.TryParse(modelValue.Substring(1, (modelValue.Length - 1)), out int brushModelIndex) && bModelMap.TryGetValue(brushModelIndex, out GameObject modelGO))
                        {
                            modelGO.transform.position = position;

                            if (skybox && importOptions.skyboxMode == SkyboxMode.Scale)
                                modelGO.transform.localScale = Vector3.one * skyCameraScale;
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

                        if (skybox && importOptions.skyboxMode == SkyboxMode.Scale)
                            instance.transform.localScale = Vector3.one * skyCameraScale;
                    }
                }

                if (importOptions.objects.HasFlag(ObjectFlags.Lights) && className.Contains("light"))
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
                        range = lightValues.w / 5;
                        intensity = lightValues.w / (type == LightType.Directional ? 200 : 10);
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

#if UNITY_EDITOR
            if (importOptions.objects.HasFlag( ObjectFlags.LightProbes))
            {
                // Ambient lighting / Light probes
                if (importOptions.objects.HasFlag( ObjectFlags.Lights ))
                {
                    LightProbeGroup probes = new GameObject("Light Probes").AddComponent<LightProbeGroup>();
                    probes.gameObject.isStatic = true;
                    probes.transform.parent = worldGo.transform;
                    ICollection<Vector3> probePositions = null;
                    if (importOptions.probeMode == LightProbeMode.UseMapProbes)
                        probePositions = new Vector3[bspFile.ldrAmbientLighting.Length];
                    else
                        probePositions = new List<Vector3>();
                    
                    for (int i = 0; i < bspFile.ldrAmbientIndices.Length; i++)
                    {
                        Leaf leaf = bspFile.leafs[i];
                        if (leaf.cluster == skyLeafCluster) continue;

                        LeafAmbientIndex indices = bspFile.ldrAmbientIndices[i];
                        if (indices.ambientSampleCount == 0) continue;  // Prevents generating probes in solid leaves

                        Bounds leafBounds = new Bounds { min = leaf.TransformMin(), max = leaf.TransformMax() };
                        Vector3 boundsSize = leafBounds.size;

                        if (importOptions.probeMode == LightProbeMode.UseMapProbes)
                        {
                            // Use BSP's randomly placed light probe positions
                            Vector3[] probeArray = probePositions as Vector3[];
                            for (int lightIndex = indices.firstAmbientSample; lightIndex < indices.firstAmbientSample + indices.ambientSampleCount; lightIndex++)
                            {
                                LeafAmbientLighting lightInfo = bspFile.ldrAmbientLighting[lightIndex];
                                Vector3 normalizedLocation = new Vector3(lightInfo.x, lightInfo.z, lightInfo.y) / 255.0f;
                                Vector3 worldLocation = leafBounds.min + new Vector3(boundsSize.x * normalizedLocation.x, boundsSize.y * normalizedLocation.y, boundsSize.z * normalizedLocation.z);
                                probeArray[lightIndex] = worldLocation;
                            }
                        }
                        else  // Generate uniform light probe positions
                        {
                            List<Vector3> probeList = probePositions as List<Vector3>;
                            Vector3 probeDistance = Vector3.one * 1.5f;
                            int maxDimensionCount = 8;
                            Vector3Int probeCounts = Vector3Int.zero;
                            probeCounts.x = Mathf.Clamp(Mathf.FloorToInt(boundsSize.x / probeDistance.x), 1, maxDimensionCount);
                            probeCounts.y = Mathf.Clamp(Mathf.FloorToInt(boundsSize.y / probeDistance.y), 1, maxDimensionCount);
                            probeCounts.z = Mathf.Clamp(Mathf.FloorToInt(boundsSize.z / probeDistance.z), 1, maxDimensionCount);

                            for (int c = 0; c < 3; c++)  // If max count exceeded, reset probe distance
                            {
                                if (probeCounts[c] == maxDimensionCount)
                                    probeDistance[c] = boundsSize[c] / probeCounts[c];
                            }

                            Vector3 padding = (boundsSize - new Vector3((probeCounts.x - 1) * probeDistance.x, (probeCounts.y - 1) * probeDistance.y, (probeCounts.z - 1) * probeDistance.z)) / 2;

                            for (int packedPos = 0; packedPos < probeCounts.x * probeCounts.y * probeCounts.z; packedPos++)
                            {
                                int x = packedPos % probeCounts.x;
                                int y = (packedPos / probeCounts.x) % probeCounts.y;
                                int z = (packedPos / probeCounts.x) / probeCounts.y;

                                Vector3 position = leafBounds.min + new Vector3(x * probeDistance.x, y * probeDistance.y, z * probeDistance.z) + padding;
                                probeList.Add(position);
                            }
                        }

                        // Set probes
                        probes.probePositions = importOptions.probeMode == LightProbeMode.UseMapProbes ? probePositions as Vector3[] : (probePositions as List<Vector3>).ToArray();
                    }
                }
            }
#endif

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
            public bool splitWorldGeometry;
            public bool setupDependencies;
            public bool importWorldColliders;
            public LightProbeMode probeMode;
            public ObjectFlags objects;
            public SkyboxMode skyboxMode;
        }
        [System.Flags]
        public enum ObjectFlags
        {
            StaticWorld = 1 << 0,
            Props = 1 << 1,
            Lights = 1 << 2,
            LightProbes = 1 << 3,
            BrushModels = 1 << 4,
            Displacements = 1 << 5,
        }
        public enum LightProbeMode
        {
            UseMapProbes,
            GenerateUnityProbes
        }
        public enum SkyboxMode
        {
            Original,
            Cull,
            Scale,
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
            public MeshData(BSP bsp, long key, BspConverter converter)
            {
                this.bsp = bsp;
                vertices = new();
                subMeshes = new();
                subMeshMap = new();
                this.key = key;
                this.converter = converter;
            }
            public bool HasGeometry => vertices.Count > 0 && subMeshes.Count > 0;
            readonly BSP bsp;
            readonly long key;
            List<WorldVertex> vertices;
            Dictionary<int, List<uint>> subMeshes;
            List<int> subMeshMap = new();
            BspConverter converter;
            public void AddFace(ref Face face, int faceIndex)
            {
                TextureInfo textureInfo = bsp.texInfo[face.textureInfo];
                TextureData textureData = bsp.texData[textureInfo.textureDataIndex];
                string materialPath = bsp.textureStringData[textureData.nameStringTableIndex].ToLower();

                if (USource.noRenderMaterials.Contains(materialPath) || USource.noCreateMaterials.Contains(materialPath))  // Don't include sky and other tool textures
                    return;

                bool isSky = converter.skyFaces.Contains(faceIndex);

                if (!subMeshes.TryGetValue(textureData.nameStringTableIndex, out List<uint> subIndices))  // Ensure submesh/indices exist
                {
                    subMeshMap.Add(textureData.nameStringTableIndex);
                    subIndices = new();
                    subMeshes[textureData.nameStringTableIndex] = subIndices;
                }

                if (converter.importOptions.objects.HasFlag( ObjectFlags.Displacements ) && face.displacementInfo != -1)  // face is a displacement
                {
                    DisplacementInfo dispInfo = bsp.dispInfo[face.displacementInfo];
                    // Triangles
                    int vertexEdgeCount = (1 << dispInfo.power) + 1;
                    int edgeCount = 1 << dispInfo.power;
                    for (int i = 0; i < edgeCount * edgeCount; i++)
                    {
                        int x = i % edgeCount;
                        int y = i / edgeCount;

                        int vIndex = y * vertexEdgeCount + x;

                        if (vIndex % 2 == 1)
                        {
                            subIndices.Add((uint)(vertices.Count + vIndex + vertexEdgeCount));
                            subIndices.Add((uint)(vertices.Count + vIndex + 1));
                            subIndices.Add((uint)(vertices.Count + vIndex));
                            subIndices.Add((uint)(vertices.Count + vIndex + vertexEdgeCount));
                            subIndices.Add((uint)(vertices.Count + vIndex + vertexEdgeCount + 1));
                            subIndices.Add((uint)(vertices.Count + vIndex + 1));
                        }
                        else
                        {
                            subIndices.Add((uint)(vertices.Count + vIndex + vertexEdgeCount));
                            subIndices.Add((uint)(vertices.Count + vIndex + vertexEdgeCount) + 1);
                            subIndices.Add((uint)(vertices.Count + vIndex));
                            subIndices.Add((uint)(vertices.Count + vIndex + vertexEdgeCount + 1));
                            subIndices.Add((uint)(vertices.Count + vIndex + 1));
                            subIndices.Add((uint)(vertices.Count + vIndex));
                        }
                    }

                    // Vertices
                    Vector3 basePosition = IConverter.SourceTransformPoint(dispInfo.startPosition);
                    // determine the closest vertex on the face
                    Vector3[] faceVertices = new Vector3[4];
                    int minimumVertexIndex = -1;
                    float minimumVertexDistance = float.MaxValue;
                    for (int surfEdgeIndex = face.firstEdgeIndex, i = 0; surfEdgeIndex < face.firstEdgeIndex + face.edgeCount; surfEdgeIndex++, i++)
                    {
                        int surfEdge = bsp.surfEdges[surfEdgeIndex];
                        int edgeIndex = surfEdge > 0 ? 
                            bsp.edges[Mathf.Abs(surfEdge)].index0 : 
                            bsp.edges[Mathf.Abs(surfEdge)].index1;

                        Vector3 vertex = EnsureFinite(bsp.vertices[edgeIndex]);
                        if (isSky && converter.importOptions.skyboxMode == SkyboxMode.Scale)
                            vertex = converter.TransformSkyboxToWorld(vertex);
                        faceVertices[i] = vertex;
                        float distance = Vector3.Distance(basePosition, vertex);
                        if (distance < minimumVertexDistance)
                        {
                            minimumVertexDistance = distance;
                            minimumVertexIndex = i;
                        }
                    }

                    int GetFaceIndex(int index) => (index + minimumVertexIndex) % 4;

                    Vector3 tS = new Vector3(-textureInfo.textureVecs0.y, textureInfo.textureVecs0.z, -textureInfo.textureVecs0.x);
                    Vector3 tT = new Vector3(-textureInfo.textureVecs1.y, textureInfo.textureVecs1.z, -textureInfo.textureVecs1.x);

                    Vector3 LeftEdge = faceVertices[GetFaceIndex(1)] - faceVertices[GetFaceIndex(0)];
                    Vector3 RightEdge = faceVertices[GetFaceIndex(2)] - faceVertices[GetFaceIndex(3)];

                    float SubdivideScale = 1.0f / edgeCount;

                    Vector3 LeftEdgeStep = LeftEdge * SubdivideScale;
                    Vector3 RightEdgeStep = RightEdge * SubdivideScale;

                    for (int x = 0; x < vertexEdgeCount; x++)
                    {
                        Vector3 LeftEnd = LeftEdgeStep * x;
                        LeftEnd += faceVertices[GetFaceIndex(0)];

                        Vector3 RightEnd = RightEdgeStep * x;
                        RightEnd += faceVertices[GetFaceIndex(3)];

                        Vector3 LeftRightSeg = RightEnd - LeftEnd;
                        Vector3 LeftRightStep = LeftRightSeg * SubdivideScale;

                        for (int z = 0; z < vertexEdgeCount; z++)
                        {
                            Int32 DispVertIndex = dispInfo.displacementVertexStart + (x * vertexEdgeCount + z);
                            DisplacementVertex DispVertInfo = bsp.dispVerts[DispVertIndex];

                            Vector3 FlatVertex = LeftEnd + (LeftRightStep * z);
                            Vector3 DispVertex = IConverter.SourceTransformDirection(DispVertInfo.displacement) * (DispVertInfo.distance * USource.settings.sourceToUnityScale);
                            DispVertex += FlatVertex;

                            if (isSky == false)
                                isSky = converter.bspFile.leafs.Any(e => e.cluster == converter.skyLeafCluster && e.Contains(DispVertex));

                            if (isSky && converter.importOptions.skyboxMode == SkyboxMode.Scale)
                                DispVertex = converter.TransformSkyboxToWorld(DispVertex);

                            float s = (Vector3.Dot(FlatVertex, tS) + textureInfo.textureVecs0.w * USource.settings.sourceToUnityScale) / (textureData.viewWidth * USource.settings.sourceToUnityScale);
                            float t = -(Vector3.Dot(FlatVertex, tT) + textureInfo.textureVecs1.w * USource.settings.sourceToUnityScale) / (textureData.viewHeight * USource.settings.sourceToUnityScale);

                            vertices.Add(new WorldVertex { position = DispVertex, uv = new half2(new Vector2(s, t)) });
                        }
                    }
                }
                else  // flat face
                {
                    for (int index = 1, k = 0; index < face.edgeCount - 1; index++, k += 3)
                    {
                        subIndices.Add((uint)vertices.Count);
                        subIndices.Add((uint)(vertices.Count + index));
                        subIndices.Add((uint)(vertices.Count + index + 1));
                    }

                    Vector3 tS = new Vector3(-textureInfo.textureVecs0.y, textureInfo.textureVecs0.z, textureInfo.textureVecs0.x);
                    Vector3 tT = new Vector3(-textureInfo.textureVecs1.y, textureInfo.textureVecs1.z, textureInfo.textureVecs1.x);

                    for (int surfEdgeIndex = face.firstEdgeIndex; surfEdgeIndex < face.firstEdgeIndex + face.edgeCount; surfEdgeIndex++)
                    {
                        int surfEdge = bsp.surfEdges[surfEdgeIndex];
                        int edgeIndex = surfEdge > 0 ?
                            bsp.edges[Mathf.Abs(surfEdge)].index0 :
                            bsp.edges[Mathf.Abs(surfEdge)].index1;

                        WorldVertex vertex = new();
                        vertex.position = EnsureFinite(bsp.vertices[edgeIndex]);
                        if (isSky && converter.importOptions.skyboxMode == SkyboxMode.Scale)
                        {
                            vertex.position = converter.TransformSkyboxToWorld(vertex.position);
                        }

                        Vector3 crossVertexPostiion = new Vector3(-vertex.position.z, vertex.position.y, vertex.position.x);
                        float TextureUVS = (Vector3.Dot(crossVertexPostiion, tS) + textureInfo.textureVecs0.w * USource.settings.sourceToUnityScale) / (textureData.viewWidth * USource.settings.sourceToUnityScale);
                        float TextureUVT = -(Vector3.Dot(crossVertexPostiion, tT) + textureInfo.textureVecs1.w * USource.settings.sourceToUnityScale) / (textureData.viewHeight * USource.settings.sourceToUnityScale);
                        vertex.uv = new half2(EnsureFinite(new Vector2(TextureUVS, TextureUVT)));

                        vertices.Add(vertex);
                    }
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
