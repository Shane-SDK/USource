using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using USource.MathLib;

namespace USource.Formats.Source.MDL
{
    public class MDLFile : StudioStruct
    {
        public bool HasPhysics => physSolids != null;
        public studiohdr_t MDL_Header;

        public String[] MDL_BoneNames;
        public mstudiobone_t[] MDL_StudioBones;

        //animations
        public mstudioseqdesc_t[] MDL_SeqDescriptions;
        public mstudioanimdesc_t[] MDL_AniDescriptions;

        public AniInfo[] Animations;
        public SeqInfo[] Sequences;
        //TODO
        //static mstudioevent_t[] MDL_Events;
        //animations

        //Materials
        public mstudiotexture_t[] MDL_TexturesInfo;
        public String[] MDL_TDirectories;
        public String[] MDL_Textures;
        //Materials

        public mstudiohitboxset_t[] MDL_Hitboxsets;
        public Hitbox[][] Hitboxes;
        public Boolean meshExist = true;

        public StudioBodyPart[] MDL_Bodyparts;
        // Physics
        public PhysSolid[] physSolids;
        public MDLFile(Stream FileInput, Stream physStream, Boolean parseAnims = false, Boolean parseHitboxes = false)
        {
            using (var FileStream = new UReader(FileInput))
            {
                FileStream.ReadType(ref MDL_Header);

                if (MDL_Header.id != 0x54534449)
                   throw new FileLoadException("File signature does not match 'IDST'");

                //Bones
                MDL_StudioBones = new mstudiobone_t[MDL_Header.bone_count];
                MDL_BoneNames = new String[MDL_Header.bone_count];
                for (Int32 boneID = 0; boneID < MDL_Header.bone_count; boneID++)
                {
                    Int32 boneOffset = MDL_Header.bone_offset + (216 * boneID);
                    FileStream.ReadType(ref MDL_StudioBones[boneID], boneOffset);
                    MDL_BoneNames[boneID] = FileStream.ReadNullTerminatedString(boneOffset + MDL_StudioBones[boneID].sznameindex);
                }
                //Bones

                if (parseHitboxes && false)
                {
                    MDL_Hitboxsets = new mstudiohitboxset_t[MDL_Header.hitbox_count];
                    Hitboxes = new Hitbox[MDL_Header.hitbox_count][];
                    for (Int32 hitboxsetID = 0; hitboxsetID < MDL_Header.hitbox_count; hitboxsetID++)
                    {
                        Int32 hitboxsetOffset = MDL_Header.hitbox_offset + (12 * hitboxsetID);
                        FileStream.ReadType(ref MDL_Hitboxsets[hitboxsetID], hitboxsetOffset);
                        Hitboxes[hitboxsetID] = new Hitbox[MDL_Hitboxsets[hitboxsetID].numhitboxes];

                        for (Int32 hitboxID = 0; hitboxID < MDL_Hitboxsets[hitboxsetID].numhitboxes; hitboxID++)
                        {
                            Int32 hitboxOffset = hitboxsetOffset + (68 * hitboxID) + MDL_Hitboxsets[hitboxsetID].hitboxindex;
                            Hitboxes[hitboxsetID][hitboxID].BBox = new mstudiobbox_t();

                            FileStream.ReadType(ref Hitboxes[hitboxsetID][hitboxID].BBox, hitboxOffset);
                        }
                    }
                }

                if (parseAnims)
                {
                    //Animations
                    MDL_AniDescriptions = new mstudioanimdesc_t[MDL_Header.localanim_count];
                    Animations = new AniInfo[MDL_Header.localanim_count];

                    for (Int32 AnimID = 0; AnimID < MDL_Header.localanim_count; AnimID++)
                    {
                        try
                        {
                            Int32 AnimOffset = MDL_Header.localanim_offset + (100 * AnimID);
                            FileStream.ReadType(ref MDL_AniDescriptions[AnimID], AnimOffset);
                            mstudioanimdesc_t StudioAnim = MDL_AniDescriptions[AnimID];
                            String StudioAnimName = FileStream.ReadNullTerminatedString(AnimOffset + StudioAnim.sznameindex);
                            Animations[AnimID] = new AniInfo { name = StudioAnimName, studioAnim = StudioAnim };
                            Animations[AnimID].AnimationBones = new List<AnimationBone>();

                            //mstudioanim_t
                            FileStream.BaseStream.Position = AnimOffset;

                            Int64 StartOffset = FileStream.BaseStream.Position;

                            Int32 CurrentOffset = MDL_AniDescriptions[AnimID].animindex;
                            Int16 NextOffset;

                            do
                            {
                                //Debug.Log($"StartOffset: {StartOffset}");
                                //Debug.Log($"CurrentOffset: {CurrentOffset}");
                                //if (StartOffset + CurrentOffset < 0)
                                //    continue;
                                if (StartOffset + CurrentOffset < 0)
                                    break;
                                FileStream.BaseStream.Position = StartOffset + CurrentOffset;
                                Byte BoneIndex = FileStream.ReadByte();
                                Byte BoneFlag = FileStream.ReadByte();
                                NextOffset = FileStream.ReadInt16();
                                CurrentOffset += NextOffset;

                                AnimationBone AnimatedBone = new AnimationBone(BoneIndex, BoneFlag, MDL_AniDescriptions[AnimID].numframes);
                                AnimatedBone.ReadData(FileStream);
                                Animations[AnimID].AnimationBones.Add(AnimatedBone);

                            } while (NextOffset != 0);
                            //mstudioanim_t

                            List<AnimationBone> AnimationBones = Animations[AnimID].AnimationBones;
                            Int32 NumBones = MDL_Header.bone_count;
                            Int32 NumFrames = StudioAnim.numframes;

                            //Used to avoid "Assertion failed" key count in Unity (if frames less than 2)
                            Boolean FramesLess = NumFrames < 2;
                            if (FramesLess)
                                NumFrames += 1;

                            Animations[AnimID].PosX = new Keyframe[NumFrames][];
                            Animations[AnimID].PosY = new Keyframe[NumFrames][];
                            Animations[AnimID].PosZ = new Keyframe[NumFrames][];

                            Animations[AnimID].RotX = new Keyframe[NumFrames][];
                            Animations[AnimID].RotY = new Keyframe[NumFrames][];
                            Animations[AnimID].RotZ = new Keyframe[NumFrames][];
                            Animations[AnimID].RotW = new Keyframe[NumFrames][];
                            for (Int32 FrameID = 0; FrameID < NumFrames; FrameID++)
                            {
                                Animations[AnimID].PosX[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].PosY[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].PosZ[FrameID] = new Keyframe[NumBones];

                                Animations[AnimID].RotX[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotY[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotZ[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotW[FrameID] = new Keyframe[NumBones];
                            }

                            for (Int32 boneID = 0; boneID < NumBones; boneID++)
                            {
                                AnimationBone AnimBone = AnimationBones.FirstOrDefault(x => x.Bone == boneID);

                                //frameIndex < 30 && studioAnimName == "@ak47_reload"
                                for (Int32 frameID = 0; frameID < NumFrames; frameID++)
                                {
                                    //get current animation time (length) by divide frame index on "fps"
                                    Single time = frameID / StudioAnim.fps;

                                    mstudiobone_t StudioBone = MDL_StudioBones[boneID];
                                    //Transform bone = Bones[boneIndex];

                                    Vector3 Position = StudioBone.pos;
                                    Vector3 Rotation = StudioBone.rot;

                                    //BINGO! All animations are corrected :p
                                    if (AnimBone != null)
                                    {
                                        if ((AnimBone.Flags & STUDIO_ANIM_RAWROT) > 0)
                                            Rotation = MathLibrary.ToEulerAngles(AnimBone.pQuat48);

                                        if ((AnimBone.Flags & STUDIO_ANIM_RAWROT2) > 0)
                                            Rotation = MathLibrary.ToEulerAngles(AnimBone.pQuat64);

                                        if ((AnimBone.Flags & STUDIO_ANIM_RAWPOS) > 0)
                                            Position = AnimBone.pVec48;

                                        if ((AnimBone.Flags & STUDIO_ANIM_ANIMROT) > 0)
                                            Rotation += AnimBone.FrameAngles[(FramesLess && frameID != 0) ? frameID - 1 : frameID].Multiply(StudioBone.rotscale);

                                        if ((AnimBone.Flags & STUDIO_ANIM_ANIMPOS) > 0)
                                            Position += AnimBone.FramePositions[(FramesLess && frameID != 0) ? frameID - 1 : frameID].Multiply(StudioBone.posscale);

                                        if ((AnimBone.Flags & STUDIO_ANIM_DELTA) > 0)
                                        {
                                            Position = Vector3.zero;
                                            Rotation = Vector3.zero;
                                        }
                                    }

                                    //Invert right-handed position to left-handed
                                    if (StudioBone.parent == -1)
                                        Position = MathLibrary.SwapY(Position);
                                    else
                                        Position.x = -Position.x;

                                    //Corrects global scale and convert radians to degrees
                                    Position *= USource.settings.sourceToUnityScale;
                                    Rotation *= Mathf.Rad2Deg;
                                    Quaternion quat;

                                    //Fix up bone rotations from right-handed to left-handed
                                    if (StudioBone.parent == -1)
                                        quat = Quaternion.Euler(-90, 180, -90) * MathLibrary.AngleQuaternion(Rotation);
                                    else
                                        quat = MathLibrary.AngleQuaternion(Rotation);

                                    Animations[AnimID].PosX[frameID][boneID] = new Keyframe(time, Position.x);
                                    Animations[AnimID].PosY[frameID][boneID] = new Keyframe(time, Position.y);
                                    Animations[AnimID].PosZ[frameID][boneID] = new Keyframe(time, Position.z);

                                    Animations[AnimID].RotX[frameID][boneID] = new Keyframe(time, quat.x);
                                    Animations[AnimID].RotY[frameID][boneID] = new Keyframe(time, quat.y);
                                    Animations[AnimID].RotZ[frameID][boneID] = new Keyframe(time, quat.z);
                                    Animations[AnimID].RotW[frameID][boneID] = new Keyframe(time, quat.w);
                                }
                            }
                        }
                        catch
                        {
                            Debug.Log($"Failed to export animation {AnimID}");
                            break;
                        }
                    }
                    //Animations

                    //Sequences
                    MDL_SeqDescriptions = new mstudioseqdesc_t[MDL_Header.localseq_count];
                    Sequences = new SeqInfo[MDL_Header.localseq_count];

                    for (Int32 seqID = 0; seqID < MDL_Header.localseq_count; seqID++)
                    {
                        Int32 sequenceOffset = MDL_Header.localseq_offset + (212 * seqID);
                        FileStream.ReadType(ref MDL_SeqDescriptions[seqID], sequenceOffset);
                        mstudioseqdesc_t Sequence = MDL_SeqDescriptions[seqID];
                        Sequences[seqID] = new SeqInfo { name = FileStream.ReadNullTerminatedString(sequenceOffset + Sequence.szlabelindex), seq = Sequence };

                        FileStream.BaseStream.Position = sequenceOffset + Sequence.animindexindex;

                        var animID = FileStream.ReadShortArray(Sequence.groupsize[0] * Sequence.groupsize[1]);
                        //Debug.LogWarning(animIndices[0]);
                        // Just use the first animation for now
                        Sequences[seqID].ani = Animations[animID[0]];
                    }
                }

                //Materials
                MDL_TexturesInfo = new mstudiotexture_t[MDL_Header.texture_count];
                MDL_Textures = new String[MDL_Header.texture_count];
                for (Int32 texID = 0; texID < MDL_Header.texture_count; texID++)
                {
                    Int32 textureOffset = MDL_Header.texture_offset + (64 * texID);
                    FileStream.ReadType(ref MDL_TexturesInfo[texID], textureOffset);
                    MDL_Textures[texID] = FileStream.ReadNullTerminatedString(textureOffset + MDL_TexturesInfo[texID].sznameindex);
                }

                Int32[] TDirOffsets = new Int32[MDL_Header.texturedir_count];
                MDL_TDirectories = new String[MDL_Header.texturedir_count];
                for (Int32 dirID = 0; dirID < MDL_Header.texturedir_count; dirID++)
                {
                    FileStream.ReadType(ref TDirOffsets[dirID], MDL_Header.texturedir_offset + (4 * dirID));
                    MDL_TDirectories[dirID] = FileStream.ReadNullTerminatedString(TDirOffsets[dirID]).Replace("\\", "/");
                }
                //Materials

                //Bodyparts
                MDL_Bodyparts = new StudioBodyPart[MDL_Header.bodypart_count];
                for (Int32 bodypartID = 0; bodypartID < MDL_Header.bodypart_count; bodypartID++)
                {
                    mstudiobodyparts_t pBodypart = new mstudiobodyparts_t();
                    Int32 pBodypartOffset = MDL_Header.bodypart_offset + (16 * bodypartID);
                    FileStream.ReadType(ref pBodypart, pBodypartOffset);

                    MDL_Bodyparts[bodypartID].Name = FileStream.ReadNullTerminatedString(pBodypartOffset + pBodypart.sznameindex);
                    MDL_Bodyparts[bodypartID].Models = new StudioModel[pBodypart.nummodels];

                    for (Int32 modelID = 0; modelID < pBodypart.nummodels; modelID++)
                    {
                        mstudiomodel_t pModel = new mstudiomodel_t();
                        Int64 pModelOffset = pBodypartOffset + (148 * modelID) + pBodypart.modelindex;
                        FileStream.ReadType(ref pModel, pModelOffset);

                        MDL_Bodyparts[bodypartID].Models[modelID].isBlank = (pModel.numvertices <= 0 || pModel.nummeshes <= 0);
                        MDL_Bodyparts[bodypartID].Models[modelID].Model = pModel;
                        MDL_Bodyparts[bodypartID].Models[modelID].Meshes = new mstudiomesh_t[pModel.nummeshes];
                        for (Int32 meshID = 0; meshID < pModel.nummeshes; meshID++)
                        {
                            mstudiomesh_t pMesh = new mstudiomesh_t();
                            Int64 pMeshOffset = pModelOffset + (116 * meshID) + pModel.meshindex;
                            FileStream.ReadType(ref pMesh, pMeshOffset);

                            MDL_Bodyparts[bodypartID].Models[modelID].Meshes[meshID] = pMesh;
                        }

                        MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod = new Dictionary<Int32, List<Int32>>[8];

                        for (Int32 i = 0; i < 8; i++)
                            MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod[i] = new Dictionary<Int32, List<Int32>>();

                        MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod = new mstudiovertex_t[8][];
                    }
                }
                //BodyParts
            }

            if (physStream != null)
            {
                const float physicsScalingFactor = 1.016f;
                using UReader reader = new UReader(physStream);

                phyheader_t header = default;
                long positionStart = reader.BaseStream.Position;
                reader.ReadType<phyheader_t>(ref header);

                if (header.solidCount == 0)
                    return;

                physSolids = new PhysSolid[header.solidCount];

                int partCount = 0;

                for (int i = 0; i < header.solidCount; i++)
                {
                    // Each solid can be made up of separate bodies of vertices
                    List<List<int>> indexSet = new List<List<int>>();
                    compactsurfaceheader_t compactHeader = default;
                    long nextHeader = reader.BaseStream.Position;
                    reader.ReadType<compactsurfaceheader_t>(ref compactHeader);
                    nextHeader += compactHeader.size + sizeof(int);

                    legacysurfaceheader_t legacyHeader = default;
                    reader.ReadType<legacysurfaceheader_t>(ref legacyHeader);

                    long verticesPosition = 0;
                    int largestVertexIndex = -1;

                    // The number of separate bodies in a solid seems to be unknown so stop once the beginning of the vertex offset is reached
                    while ((reader.BaseStream.Position < verticesPosition || largestVertexIndex == -1) && reader.BaseStream.Position < reader.BaseStream.Length)  // Read triangles until the vertex offset is reached
                    {
                        partCount++;
                        List<int> indices = new List<int>();
                        indexSet.Add(indices);
                        trianglefaceheader_t triangleFaceHeader = default;
                        long headerPosition = reader.BaseStream.Position;
                        reader.ReadType<trianglefaceheader_t>(ref triangleFaceHeader);

                        verticesPosition = headerPosition + triangleFaceHeader.m_offsetTovertices;

                        //BitArray bitSet = new BitArray(triangleFaceHeader.dummy[1]);
                        //bool skipData = bitSet[0];

                        //if (skipData)
                        //    break;

                        triangleface_t[] triangleFaces = new triangleface_t[triangleFaceHeader.m_countFaces];
                        reader.ReadArray<triangleface_t>(ref triangleFaces);

                        for (int t = 0; t < triangleFaces.Length; t++)
                        {
                            triangleface_t face = triangleFaces[t];
                            indices.Add(face.v3);
                            indices.Add(face.v2);
                            indices.Add(face.v1);

                            // Get the largest vertex index
                            int max = Mathf.Max(face.v1, face.v2, face.v3);
                            if (max > largestVertexIndex)
                                largestVertexIndex = max;
                        }
                    }

                    vertex[] vertices = new vertex[largestVertexIndex + 1];
                    reader.ReadArray<vertex>(ref vertices, verticesPosition);

                    // If there are multiple parts in a solid, there seems to be a convex version of the parts
                    // as the last part in the set -- ignore it

                    int realPartCount = indexSet.Count == 1 ? 1 : indexSet.Count - 1;  

                    PhysSolid solid = default;
                    solid.index = -1;
                    solid.parts = new PhysPart[realPartCount];
                    for (int p = 0; p < realPartCount; p++)
                        solid.parts[p].triangles = indexSet[p].ToArray();

                    solid.vertices = new Vector3[vertices.Length];

                    bool isStatic = MDL_Header.flags.HasFlag(StudioHDRFlags.STUDIOHDR_FLAGS_STATIC_PROP);
                    for (int t = 0; t < vertices.Length; t++)
                    {
                        solid.vertices[t] = Quaternion.AngleAxis(180, Vector3.up) * Quaternion.AngleAxis(isStatic ? 90 : 0, Vector3.right) * new Vector3(
                            vertices[t].position[0],
                            vertices[t].position[2],
                            vertices[t].position[1]) / physicsScalingFactor;
                    }

                    this.physSolids[i] = solid;
                    reader.BaseStream.Position = nextHeader;
                }

                // Read text at the end of file
                byte[] bytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                string text = System.Text.Encoding.ASCII.GetString(bytes);
                KeyValues keyValues = KeyValues.Parse(text);
                foreach (KeyValues.Entry entry in keyValues["solid"])
                {
                    if (int.TryParse(entry["index"], out int index))
                    {
                        PhysSolid solid = physSolids[index];
                        solid.index = index;
                        if (float.TryParse(entry["mass"], out float mass))
                        {
                            solid.mass = mass;
                        }
                        solid.boneName = entry["name"];

                        physSolids[index] = solid;
                    }
                }
            }
        }
        public void SetIndices(Int32 BodypartID, Int32 ModelID, Int32 LODID, Int32 MeshID, List<Int32> Indices)
        {
            MDL_Bodyparts[BodypartID].Models[ModelID].IndicesPerLod[LODID].Add(MeshID, Indices);
        }
        public void SetVertices(Int32 BodypartID, Int32 ModelID, Int32 LODID, Int32 TotalVerts, Int32 StartIndex, mstudiovertex_t[] Vertexes)
        {
            MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID] = new mstudiovertex_t[TotalVerts];
            Array.Copy(Vertexes, StartIndex, MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID], 0, TotalVerts);
        }
        static String HitTagType(Int32 typeHit)
        {
            String returnType;
            switch (typeHit)
            {
                case 1: // - Used for human NPC heads and to define where the player sits on the vehicle.mdl, appears Red in HLMV
                    returnType = "Head";
                    break;

                case 2: // - Used for human NPC midsection and chest, appears Green in HLMV
                    returnType = "Chest";
                    break;

                case 3: // - Used for human NPC stomach and pelvis, appears Yellow in HLMV
                    returnType = "Stomach";
                    break;

                case 4: // - Used for human Left Arm, appears Deep Blue in HLMV
                    returnType = "Left_Arm";
                    break;

                case 5: // - Used for human Right Arm, appears Bright Violet in HLMV
                    returnType = "Right_Arm";
                    break;

                case 6: // - Used for human Left Leg, appears Bright Cyan in HLMV
                    returnType = "Left_Leg";
                    break;

                case 7: // - Used for human Right Leg, appears White like the default group in HLMV
                    returnType = "Right_Leg";
                    break;

                case 8: // - Used for human neck (to fix penetration to head from behind), appears Orange in HLMV (in all games since Counter-Strike: Global Offensive)
                    returnType = "Neck";
                    break;

                default: // - the default group of hitboxes, appears White in HLMV
                    returnType = "Generic";
                    break;
            }
            return returnType;
        }
        public BoneWeight GetBoneWeight(mstudioboneweight_t mBoneWeight)
        {
            BoneWeight boneWeight = new BoneWeight();

            boneWeight.boneIndex0 = mBoneWeight.bone[0];
            boneWeight.boneIndex1 = mBoneWeight.bone[1];
            boneWeight.boneIndex2 = mBoneWeight.bone[2];

            boneWeight.weight0 = mBoneWeight.weight[0];
            boneWeight.weight1 = mBoneWeight.weight[1];
            boneWeight.weight2 = mBoneWeight.weight[2];

            return boneWeight;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct phyheader_t
        {
            public int size;           // Size of this header section (generally 16)
            public int id;             // Often zero, unknown purpose.
            public int solidCount;     // Number of solids in file
            public int checkSum;   // checksum of source .mdl file (4-bytes)
        };

        // new phy format
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct compactsurfaceheader_t
        {
            public int size;           // Size of the content after this byte
            public int vphysicsID;     // Generally the ASCII for "VPHY" in newer files
            public short version;
            public short modelType;
            public int surfaceSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] dragAxisAreas;
            public int axisMapSize;
        };

        // old style phy format
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct legacysurfaceheader_t
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] m_vecMassCenter;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] m_vecRotationInertia;
            public float m_flUpperLimitRadius;
            public int m_volumeFull;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] dummy;     // dummy[3] is "IVPS" or 0
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct trianglefaceheader_t
        {
            public int m_offsetTovertices; // + address of this block = beginn of the vertices
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public int[] dummy;
            public int m_countFaces;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct triangleface_t
        {
            public byte id;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] _dummy;
            public byte v1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] _dummy2;
            public byte v2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] _dummy3;
            public byte v3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] _dummy4;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct vertex
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] position;
            int unknown;
        };
    }
}