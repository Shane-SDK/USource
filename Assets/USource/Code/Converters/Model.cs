using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static USource.Formats.Source.MDL.StudioStruct;
using USource.Formats.Source.MDL;
using USource.MathLib;
using System.IO;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace USource.Converters
{
    public class Model : Converter
    {
        public enum ImportOptions
        {
            Geometry = 1 << 0,
            Animations = 1 << 1,
            Physics = 1 << 2,
            Hitboxes = 1 << 3,
        }
        public readonly static VertexAttributeDescriptor[] staticVertexDescriptor = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1),
        };
        public readonly static VertexAttributeDescriptor[] skinnedVertexDescriptor = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1),
            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float16, 4, 2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt16, 4, 2),
        };
        public readonly static VertexAttributeDescriptor[] physVertexDescriptor = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        };
        public Formats.Source.MDL.MDLFile mdl;
        public List<AnimationClip> clips;
        public readonly ImportOptions importOptions;
        public Model(string sourcePath, Stream stream, Stream vvdStream, Stream vtxStream, Stream physStream, ImportOptions importOptions) : base(sourcePath, stream)
        {
            stream.Position = 0;
            mdl = new MDLFile(stream, physStream, true);

            if (vvdStream != null && vtxStream != null)
            {
                new VTXFile(vtxStream, mdl, new VVDFile(vvdStream, mdl));
            }

            this.importOptions = importOptions;
        }
        public override UnityEngine.Object CreateAsset()
        {
            bool isStatic = mdl.MDL_Header.flags.HasFlag(StudioHDRFlags.STUDIOHDR_FLAGS_STATIC_PROP);

            GameObject model = new GameObject(mdl.MDL_Header.Name);

            Transform[] Bones = new Transform[mdl.MDL_Header.bone_count];
            Dictionary<Int32, String> bonePathDict = new Dictionary<Int32, String>();

            if (isStatic == false || true)
            {
                for (Int32 boneID = 0; boneID < mdl.MDL_Header.bone_count; boneID++)
                {
                    GameObject BoneObject = new GameObject(mdl.MDL_BoneNames[boneID]);

                    Bones[boneID] = BoneObject.transform;//MDL_Bones.Add(BoneObject.transform);

                    Vector3 pos = mdl.MDL_StudioBones[boneID].pos * USource.settings.sourceToUnityScale;
                    Vector3 rot = mdl.MDL_StudioBones[boneID].rot * Mathf.Rad2Deg;

                    //Invert x for convert right-handed to left-handed
                    pos.x = -pos.x;

                    if (mdl.MDL_StudioBones[boneID].parent >= 0)
                    {
                        Bones[boneID].parent = Bones[mdl.MDL_StudioBones[boneID].parent];
                    }
                    else
                    {
                        //Swap Z & Y and invert Y (ex: X Z -Y)
                        //only for parents, cuz parents used different order vectors
                        float temp = pos.y;
                        pos.y = pos.z;
                        pos.z = -temp;

                        Bones[boneID].parent = model.transform;
                    }

                    bonePathDict.Add(boneID, Bones[boneID].GetTransformPath(model.transform));

                    Bones[boneID].localPosition = pos;
                    //Bones[boneID].localRotation = MDL_StudioBones[i].quat;

                    if (mdl.MDL_StudioBones[boneID].parent == -1)
                    {
                        //Fix up parents
                        Bones[boneID].localRotation = Quaternion.Euler(-90, 90, -90) * MathLibrary.AngleQuaternion(rot);
                    }
                    else
                        Bones[boneID].localRotation = MathLibrary.AngleQuaternion(rot);
                }
            }

            if (importOptions.HasFlag(ImportOptions.Hitboxes) && mdl.MDL_Hitboxsets != null)
            {
                for (Int32 hitboxsetID = 0; hitboxsetID < mdl.MDL_Header.hitbox_count; hitboxsetID++)
                {
                    for (Int32 hitboxID = 0; hitboxID < mdl.MDL_Hitboxsets[hitboxsetID].numhitboxes; hitboxID++)
                    {
                        mstudiobbox_t hitbox = mdl.Hitboxes[hitboxsetID][hitboxID].BBox;
                        BoxCollider bbox = new GameObject(String.Format("Hitbox_{0}", Bones[hitbox.bone].name)).AddComponent<BoxCollider>();

                        bbox.size = MathLibrary.NegateX(hitbox.bbmax - hitbox.bbmin) * USource.settings.sourceToUnityScale;
                        bbox.center = (MathLibrary.NegateX(hitbox.bbmax + hitbox.bbmin) / 2) * USource.settings.sourceToUnityScale;

                        bbox.transform.parent = Bones[hitbox.bone];
                        bbox.transform.localPosition = Vector3.zero;
                        bbox.transform.localRotation = Quaternion.identity;

                        //bbox.transform.tag = HitTagType(MDL_BBoxes[i].group);
                    }
                }
            }

            if (importOptions.HasFlag(ImportOptions.Geometry) && mdl.meshExist)
            {
                // Merge body groups into one mesh

                bool hasArmature = mdl.MDL_Header.bone_count > 1;
                List<UnityEngine.Material> materials = new List<UnityEngine.Material>();

                List<Vertex> vertices = new();
                List<SkinnedVertexData> skinnedData = hasArmature ? new() : null;
                List<Vector2> uvs = new();
                Dictionary<int, (List<int>, int)> submeshes = new();  // (Indices, length)
                int indexOffset = 0;

                // For each bodygroup slot
                for (Int32 bodypartID = 0; bodypartID < mdl.MDL_Header.bodypart_count; bodypartID++)
                {
                    StudioBodyPart BodyPart = mdl.MDL_Bodyparts[bodypartID];

                    int modelID = 0;

                    StudioModel Model = BodyPart.Models[modelID];

                    //Skip if model is blank
                    if (Model.isBlank)
                        continue;

                    mstudiovertex_t[] Vertexes = Model.VerticesPerLod[0];

                    for (Int32 i = 0; i < Vertexes.Length; i++)
                    {
                        Vector3 position = SourceTransformPoint(Vertexes[i].m_vecPosition);
                        Vector3 normal = SourceTransformDirection(Vertexes[i].m_vecNormal);
                        Vector2 uv = Vertexes[i].m_vecTexCoord;

                        vertices.Add(new Vertex
                        {
                            normal = normal,
                            position = position
                        });

                        uvs.Add(uv);

                        if (hasArmature)  // AaaaAAAAUUUGH
                        {
                            BoneWeight weight = mdl.GetBoneWeight(Vertexes[i].m_BoneWeights);
                            //skinnedData.Add(new SkinnedVertexData
                            //{
                            //    boneIndices = new uint4((uint)weight.boneIndex0, (uint)weight.boneIndex1, (uint)weight.boneIndex2, (uint)weight.boneIndex3),
                            //    boneWeights = new float4(weight.weight0, weight.weight1, weight.weight2, weight.weight3)
                            //});
                            skinnedData.Add(new SkinnedVertexData
                            {
                                boneIndices =
                                    ((ushort)weight.boneIndex0) |
                                    ((ulong)(ushort)weight.boneIndex1 << 16) |
                                    ((ulong)(ushort)weight.boneIndex2 << 32) |
                                    ((ulong)(ushort)weight.boneIndex3 << 48),
                                boneWeights = new half4((half)weight.weight0, (half)weight.weight1, (half)weight.weight2, (half)weight.weight3)
                            });
                        }
                    }

                    for (Int32 meshID = 0; meshID < Model.Model.nummeshes; meshID++)  // For each material slot
                    {
                        // Get the appropriate submesh
                        int submeshIndex = Model.Meshes[meshID].material;

                        if (submeshes.TryGetValue(submeshIndex, out (List<int>, int) submesh) == false)
                        {
                            submesh = (new List<int>(), Model.IndicesPerLod[0][meshID].Count);
                            submeshes[submeshIndex] = submesh;
                        }

                        for (int i = 0; i < Model.IndicesPerLod[0][meshID].Count; i++)  // For each index
                        {
                            submesh.Item1.Add(Model.IndicesPerLod[0][meshID][i] + indexOffset);
                        }

                        // Load material
                        for (Int32 DirID = 0; DirID < mdl.MDL_TDirectories.Length; DirID++)
                        {
                            string sourceMaterialPath = "materials/" + mdl.MDL_TDirectories[DirID] + mdl.MDL_Textures[submeshIndex] + ".vmt";
                            if (USource.ResourceManager.GetUnityObjectFromCache(new Location(sourceMaterialPath, Location.Type.Source), out UnityEngine.Material resource))
                            {
                                materials.Add(resource);
                                break;
                            }
                        }
                    }

                    indexOffset = vertices.Count;
                }

                Mesh mesh = new Mesh();
                mesh.name = mdl.MDL_Header.Name;
                mesh.subMeshCount = submeshes.Count;
                SubMeshDescriptor[] subMeshDescriptors = new SubMeshDescriptor[submeshes.Count];

                mesh.SetVertexBufferParams(vertices.Count, hasArmature ? skinnedVertexDescriptor : staticVertexDescriptor);
                mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count, 0);
                mesh.SetVertexBufferData(uvs, 0, 0, uvs.Count, 1);

                if (hasArmature)
                    mesh.SetVertexBufferData(skinnedData, 0, 0, skinnedData.Count, 2);

                IndexFormat indexFormat = vertices.Count >= (1 << 16) ? IndexFormat.UInt32 : IndexFormat.UInt16;

                IList indices = indexFormat == IndexFormat.UInt32 ? new List<uint>() : new List<ushort>();
                foreach (KeyValuePair<int, (List<int>, int)> submesh in submeshes)
                {
                    subMeshDescriptors[submesh.Key] = new SubMeshDescriptor(indices.Count, submesh.Value.Item2);
                    if (indexFormat == IndexFormat.UInt16)
                    {
                        List<ushort> castList = indices as List<ushort>;
                        foreach (int index in submesh.Value.Item1)
                            castList.Add((ushort)index);
                    }
                    else
                    {
                        List<uint> castList = indices as List<uint>;
                        foreach (int index in submesh.Value.Item1)
                            castList.Add((uint)index);
                    }
                }

                mesh.SetIndexBufferParams(indices.Count, indexFormat);
                if (indexFormat == IndexFormat.UInt16)
                    mesh.SetIndexBufferData(indices as List<ushort>, 0, 0, indices.Count);
                else
                    mesh.SetIndexBufferData(indices as List<uint>, 0, 0, indices.Count);

                mesh.SetSubMeshes(subMeshDescriptors);

                Renderer renderer;

                //Renderer Renderer;
                if (mdl.MDL_Header.flags.HasFlag(StudioHDRFlags.STUDIOHDR_FLAGS_STATIC_PROP) == false)
                {
                    if (model.TryGetComponent(out renderer) == false)
                        renderer = model.AddComponent<SkinnedMeshRenderer>();
                    Matrix4x4[] BindPoses = new Matrix4x4[Bones.Length];

                    for (Int32 i = 0; i < BindPoses.Length; i++)
                        BindPoses[i] = Bones[i].worldToLocalMatrix * model.transform.localToWorldMatrix;

                    mesh.bindposes = BindPoses;

                    SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
                    skinnedRenderer.sharedMesh = mesh;
                    skinnedRenderer.bones = Bones;
                    skinnedRenderer.updateWhenOffscreen = true;
                }
                else  // Static prop / No bones
                {
                    MeshFilter MeshFilter = model.AddComponent<MeshFilter>();
                    renderer = model.AddComponent<MeshRenderer>();
                    MeshFilter.sharedMesh = mesh;
                }

                mesh.RecalculateBounds();
                mesh.Optimize();
                mesh.UploadMeshData(true);
                renderer.sharedMaterials = materials.ToArray();
            }

            if (importOptions.HasFlag(ImportOptions.Animations) && mdl.MDL_SeqDescriptions != null)
            {
                clips = new List<AnimationClip>(mdl.MDL_SeqDescriptions.Length);
                for (Int32 seqID = 0; seqID < mdl.MDL_SeqDescriptions.Length; seqID++)
                {
                    SeqInfo Sequence = mdl.Sequences[seqID];
                    AniInfo Animation = Sequence.ani;

                    //Creating "AnimationCurve" for animation "paths" (aka frames where stored position (XYZ) & rotation (XYZW))
                    AnimationCurve[] posX = new AnimationCurve[mdl.MDL_Header.bone_count];    //X
                    AnimationCurve[] posY = new AnimationCurve[mdl.MDL_Header.bone_count];    //Y
                    AnimationCurve[] posZ = new AnimationCurve[mdl.MDL_Header.bone_count];    //Z

                    AnimationCurve[] rotX = new AnimationCurve[mdl.MDL_Header.bone_count];    //X
                    AnimationCurve[] rotY = new AnimationCurve[mdl.MDL_Header.bone_count];    //Y
                    AnimationCurve[] rotZ = new AnimationCurve[mdl.MDL_Header.bone_count];    //Z
                    AnimationCurve[] rotW = new AnimationCurve[mdl.MDL_Header.bone_count];    //W

                    //Fill "AnimationCurve" arrays
                    for (Int32 boneIndex = 0; boneIndex < mdl.MDL_Header.bone_count; boneIndex++)
                    {
                        posX[boneIndex] = new AnimationCurve();
                        posY[boneIndex] = new AnimationCurve();
                        posZ[boneIndex] = new AnimationCurve();

                        rotX[boneIndex] = new AnimationCurve();
                        rotY[boneIndex] = new AnimationCurve();
                        rotZ[boneIndex] = new AnimationCurve();
                        rotW[boneIndex] = new AnimationCurve();
                    }

                    Int32 numFrames = Animation.studioAnim.numframes;

                    //Used to avoid "Assertion failed" key count in Unity (if frames less than 2)
                    if (numFrames < 2)
                        numFrames += 1;

                    //Create animation clip
                    AnimationClip clip = new AnimationClip();
                    //Make it for legacy animation system (for now, but it possible to rework for Mecanim)
                    clip.legacy = true;
                    //Set animation clip name
                    clip.name = Animation.name;

                    //To avoid problems with "obfuscators" / "protectors" for models, make sure if model have name in sequence
                    if (String.IsNullOrEmpty(clip.name))
                        clip.name = "(empty)" + seqID;

                    for (Int32 frameIndex = 0; frameIndex < numFrames; frameIndex++)
                    {
                        //Get current frame from blend (meaning from "Animation") by index
                        //AnimationFrame frame = Animation.Frames[frameIndex];

                        //Set keys (position / rotation) from current frame
                        for (Int32 boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                        {
                            posX[boneIndex].AddKey(Animation.PosX[frameIndex][boneIndex]);
                            posY[boneIndex].AddKey(Animation.PosY[frameIndex][boneIndex]);
                            posZ[boneIndex].AddKey(Animation.PosZ[frameIndex][boneIndex]);

                            rotX[boneIndex].AddKey(Animation.RotX[frameIndex][boneIndex]);
                            rotY[boneIndex].AddKey(Animation.RotY[frameIndex][boneIndex]);
                            rotZ[boneIndex].AddKey(Animation.RotZ[frameIndex][boneIndex]);
                            rotW[boneIndex].AddKey(Animation.RotW[frameIndex][boneIndex]);

                            //Set default pose from the first animation
                            if (seqID == 0 && frameIndex == 0)
                            {
                                Bones[boneIndex].localPosition = new Vector3
                                (
                                    Animation.PosX[0][boneIndex].value,
                                    Animation.PosY[0][boneIndex].value,
                                    Animation.PosZ[0][boneIndex].value
                                );

                                Bones[boneIndex].localRotation = new Quaternion
                                (
                                    Animation.RotX[0][boneIndex].value,
                                    Animation.RotY[0][boneIndex].value,
                                    Animation.RotZ[0][boneIndex].value,
                                    Animation.RotW[0][boneIndex].value
                                );
                            }
                        }
                    }

                    //Apply animation paths (Position / Rotation) to clip
                    for (Int32 boneIndex = 0; boneIndex < mdl.MDL_Header.bone_count; boneIndex++)
                    {
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localPosition.x", posX[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localPosition.y", posY[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localPosition.z", posZ[boneIndex]);

                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.x", rotX[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.y", rotY[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.z", rotZ[boneIndex]);
                        clip.SetCurve(bonePathDict[boneIndex], typeof(Transform), "localRotation.w", rotW[boneIndex]);
                    }

                    if (Animation.studioAnim.fps > 0.0f)
                        clip.frameRate = Animation.studioAnim.fps;

                    //This ensures a smooth interpolation (corrects the problem of "jitter" after 180~270 degrees rotation path)
                    //can be "comment" if have idea how to replace this
                    clip.EnsureQuaternionContinuity();
                    clips.Add(clip);
                }
            }

            if (importOptions.HasFlag(ImportOptions.Physics) && mdl.HasPhysics)
            {
                for (int s = 0; s < mdl.physSolids.Length; s++)
                {
                    PhysSolid solid = mdl.physSolids[s];
                    GameObject go = model;
                    Transform transform = model.transform.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.ToLower() == solid.boneName.ToLower());
                    if (transform != null)
                        go = transform.gameObject;
                    for (int p = 0; p < solid.parts.Length; p++)
                    {
                        if (solid.IsBoxShape(p, out Vector3 boxCenter, out Vector3 boxSize))
                        {
                            BoxCollider col = go.AddComponent<BoxCollider>();
                            col.center = boxCenter;
                            col.size = boxSize;
                        }
                        else  // Create mesh
                        {
                            
                            PhysPart part = solid.parts[p];
                            Mesh mesh = new Mesh();

                            Dictionary<Vector3, int> indexMap = new();
                            List<PhysVertex> vertices = new();
                            int[] indices = new int[part.triangles.Length];
                            Vector3 boundsMin = Vector3.one * float.MaxValue;
                            Vector3 boundsMax = Vector3.one * float.MinValue;

                            for (int i = 0; i < indices.Length; i++)
                            {
                                // get vertex this index corresponds to
                                Vector3 vertPosition = solid.vertices[part.triangles[i]];
                                if (indexMap.TryGetValue(vertPosition, out int newIndex))  // re using a vertex, use existing indices
                                {
                                    indices[i] = newIndex;
                                }
                                else  // New index (vertex) is being used, add to vertices
                                {
                                    indexMap[vertPosition] = vertices.Count;
                                    indices[i] = vertices.Count;
                                    vertices.Add(new PhysVertex { position = vertPosition });
                                    boundsMin = Vector3.Min(boundsMin, vertPosition);
                                    boundsMax = Vector3.Max(boundsMax, vertPosition);
                                }
                            }

                            // Calculate Normals
                            // Get normal for each triangle and add to each vertex' normal
                            // Normalize at the end
                            for (int i = 0; i < indices.Length / 3; i++)
                            {
                                int index0 = indices[i * 3];
                                int index1 = indices[i * 3 + 1];
                                int index2 = indices[i * 3 + 2];
                                Vector3 normal = Vector3.Cross(vertices[index1].position - vertices[index0].position, vertices[index2].position - vertices[index0].position).normalized;
                                half4 halfNormal = new half4((half)normal.x, (half)normal.y, (half)normal.z, default);

                                void AddNormal(int o)
                                {
                                    PhysVertex vertex = vertices[o];
                                    vertex.normal += normal;
                                    vertices[o] = vertex;
                                }

                                AddNormal(index0);
                                AddNormal(index1);
                                AddNormal(index2);
                            }
                            // Normalize normals
                            for (int i = 0; i < vertices.Count; i++)
                            {
                                PhysVertex vertex = vertices[i];
                                vertex.normal = vertex.normal.normalized;
                                vertices[i] = vertex;
                            }
                            mesh.name = $"{solid.boneName}.phy";

                            MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers;

                            mesh.SetVertexBufferParams(vertices.Count, physVertexDescriptor);
                            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count, 0, flags);
                            mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
                            mesh.SetIndexBufferData(indices, 0, 0, indices.Length, flags);
                            mesh.SetSubMesh(0, new SubMeshDescriptor { indexCount = indices.Length, bounds = new Bounds((boundsMin + boundsMax) / 2, boundsMax - boundsMin) }, flags);
                            mesh.bounds = new Bounds((boundsMin + boundsMax) / 2, boundsMax - boundsMin);

                            //mesh.Optimize();
                            mesh.UploadMeshData(true);

                            MeshCollider c = go.AddComponent<MeshCollider>();
                            c.sharedMesh = mesh;
                            c.convex = true;
                        }
                    }

                    go.AddComponent<Rigidbody>().mass = solid.mass;
                }
            }

            unityObject = model;

            return model;
        }
        public override IEnumerable<string> GetSourceAssetDependencies()
        {
            yield return this.sourcePath.Replace(".mdl", ".vvd");
            yield return this.sourcePath.Replace(".mdl", ".vtx");
            yield return this.sourcePath.Replace(".mdl", ".sw.vtx");
            yield return this.sourcePath.Replace(".mdl", ".phy");

            foreach (string dir in mdl.MDL_TDirectories)
            {
                foreach (string name in mdl.MDL_Textures)
                {
                    yield return $"materials/{dir}{name}.vmt";
                }
            }
            //foreach (StudioBodyPart bodyPart in mdl.MDL_Bodyparts)
            //{
            //    foreach (StudioModel model in bodyPart.Models)
            //    {
            //        foreach (mstudiomesh_t mesh in model.Meshes)
            //        {
            //            for (int dir = 0; dir < mdl.MDL_TDirectories.Length; dir++)
            //            {
            //                string texturePath = $"materials/{mdl.MDL_TDirectories[dir]}{mdl.MDL_Textures[mesh.material]}.vmt";

            //                yield return texturePath;
            //            }
            //        }
            //    }
            //}
        }
        public override Texture2D CreatePreviewTexture()
        {
            return UnityEditor.AssetPreview.GetAssetPreview(CreateAsset());
        }
        public override void SaveToAssetDatabase(UnityEngine.Object obj)
        {
            base.SaveToAssetDatabase(obj);

            string assetDatabasePath = AssetDatabasePath;

            // If the asset already exists, remove sub assets
            foreach (UnityEngine.Object ob in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetDatabasePath))
            {
                AssetDatabase.RemoveObjectFromAsset(ob);
            }

            GameObject originalGO = obj as GameObject;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(originalGO, assetDatabasePath);

            GameObject prefabGO = (UnityEditor.AssetDatabase.LoadAssetAtPath(assetDatabasePath, typeof(UnityEngine.Object)) as GameObject);

            if (originalGO.TryGetComponent<MeshFilter>(out MeshFilter originalMeshFilter))  // If the model is a static prop
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(originalMeshFilter.sharedMesh, assetDatabasePath);
                prefabGO.GetComponent<MeshFilter>().sharedMesh = originalMeshFilter.sharedMesh;
            }
            else if (originalGO.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinnedMesh)) // Bones
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(skinnedMesh.sharedMesh, assetDatabasePath);
                prefabGO.GetComponent<SkinnedMeshRenderer>().sharedMesh = skinnedMesh.sharedMesh;
            }

            MeshCollider[] colliders = originalGO.GetComponentsInChildren<MeshCollider>();
            for (int c = 0; c < colliders.Length; c++)
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(colliders[c].sharedMesh, assetDatabasePath);

                prefabGO.GetComponentsInChildren<MeshCollider>()[c].sharedMesh = colliders[c].sharedMesh;
            }

            if (clips != null)
            {
                foreach (AnimationClip clip in clips)
                {
                    AssetDatabase.AddObjectToAsset(clip, assetDatabasePath);
                }
            }

            GameObject.DestroyImmediate(originalGO);
        }
    }

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    }
    public struct SkinnedVertexData
    {
        public half4 boneWeights;
        public ulong boneIndices;
    }
    public struct PhysVertex
    {
        public Vector3 position;
        public Vector3 normal;
    }
}
