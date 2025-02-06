using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using USource.Formats.Source.PHYS;
using USource.MathLib;

namespace USource.Formats.Source.MDL
{
    public class MDLFile
    {
        public bool HasPhysics => phys != null;
        public StudioHeader MDL_Header;

        public string[] MDL_BoneNames;
        public StudioBone[] MDL_StudioBones;

        //animations
        public StudioSeqDesc[] MDL_SeqDescriptions;
        public StudioAnimDesc[] MDL_AniDescriptions;

        public AniInfo[] Animations;
        public SeqInfo[] Sequences;
        //TODO
        //static mstudioevent_t[] MDL_Events;
        //animations

        //Materials
        public StudioTexture[] MDL_TexturesInfo;
        public string[] MDL_TDirectories;
        public string[] MDL_Textures;
        //Materials

        public StudioHitboxSet[] MDL_Hitboxsets;
        public Hitbox[][] Hitboxes;
        public bool meshExist = true;

        public StudioBodyPart[] MDL_Bodyparts;

        // Physics
        public PhysFile phys;

        public MDLFile(Stream mdlStream, Stream physStream, bool parseAnims = false, bool parseHitboxes = false)
        {
            using (var reader = new UReader(mdlStream))
            {
                reader.ReadType(ref MDL_Header);

                if (MDL_Header.id != 0x54534449)
                   throw new FileLoadException("File signature does not match 'IDST'");

                //Bones
                MDL_StudioBones = new StudioBone[MDL_Header.bone_count];
                MDL_BoneNames = new string[MDL_Header.bone_count];
                for (int boneID = 0; boneID < MDL_Header.bone_count; boneID++)
                {
                    int boneOffset = MDL_Header.bone_offset + (216 * boneID);
                    reader.ReadType(ref MDL_StudioBones[boneID], boneOffset);
                    MDL_BoneNames[boneID] = reader.ReadNullTerminatedString(boneOffset + MDL_StudioBones[boneID].sznameindex);
                }
                //Bones

                if (parseHitboxes && false)
                {
                    MDL_Hitboxsets = new StudioHitboxSet[MDL_Header.hitbox_count];
                    Hitboxes = new Hitbox[MDL_Header.hitbox_count][];
                    for (int hitboxsetID = 0; hitboxsetID < MDL_Header.hitbox_count; hitboxsetID++)
                    {
                        int hitboxsetOffset = MDL_Header.hitbox_offset + (12 * hitboxsetID);
                        reader.ReadType(ref MDL_Hitboxsets[hitboxsetID], hitboxsetOffset);
                        Hitboxes[hitboxsetID] = new Hitbox[MDL_Hitboxsets[hitboxsetID].numhitboxes];

                        for (int hitboxID = 0; hitboxID < MDL_Hitboxsets[hitboxsetID].numhitboxes; hitboxID++)
                        {
                            int hitboxOffset = hitboxsetOffset + (68 * hitboxID) + MDL_Hitboxsets[hitboxsetID].hitboxindex;
                            Hitboxes[hitboxsetID][hitboxID].BBox = new StudioBBox();

                            reader.ReadType(ref Hitboxes[hitboxsetID][hitboxID].BBox, hitboxOffset);
                        }
                    }
                }

                if (parseAnims)
                {
                    //Animations
                    MDL_AniDescriptions = new StudioAnimDesc[MDL_Header.localanim_count];
                    Animations = new AniInfo[MDL_Header.localanim_count];

                    for (int AnimID = 0; AnimID < MDL_Header.localanim_count; AnimID++)
                    {
                        try
                        {
                            int AnimOffset = MDL_Header.localanim_offset + (100 * AnimID);
                            reader.ReadType(ref MDL_AniDescriptions[AnimID], AnimOffset);
                            StudioAnimDesc StudioAnim = MDL_AniDescriptions[AnimID];
                            string StudioAnimName = reader.ReadNullTerminatedString(AnimOffset + StudioAnim.sznameindex);
                            Animations[AnimID] = new AniInfo { name = StudioAnimName, studioAnim = StudioAnim };
                            Animations[AnimID].AnimationBones = new List<AnimationBone>();

                            //mstudioanim_t
                            reader.BaseStream.Position = AnimOffset;

                            long StartOffset = reader.BaseStream.Position;

                            int CurrentOffset = MDL_AniDescriptions[AnimID].animindex;
                            short NextOffset;

                            do
                            {
                                //Debug.Log($"StartOffset: {StartOffset}");
                                //Debug.Log($"CurrentOffset: {CurrentOffset}");
                                //if (StartOffset + CurrentOffset < 0)
                                //    continue;
                                if (StartOffset + CurrentOffset < 0)
                                    break;
                                reader.BaseStream.Position = StartOffset + CurrentOffset;
                                byte BoneIndex = reader.ReadByte();
                                byte BoneFlag = reader.ReadByte();
                                NextOffset = reader.ReadInt16();
                                CurrentOffset += NextOffset;

                                AnimationBone AnimatedBone = new AnimationBone(BoneIndex, BoneFlag, MDL_AniDescriptions[AnimID].numframes);
                                AnimatedBone.ReadData(reader);
                                Animations[AnimID].AnimationBones.Add(AnimatedBone);

                            } while (NextOffset != 0);
                            //mstudioanim_t

                            List<AnimationBone> AnimationBones = Animations[AnimID].AnimationBones;
                            int NumBones = MDL_Header.bone_count;
                            int NumFrames = StudioAnim.numframes;

                            //Used to avoid "Assertion failed" key count in Unity (if frames less than 2)
                            bool FramesLess = NumFrames < 2;
                            if (FramesLess)
                                NumFrames += 1;

                            Animations[AnimID].PosX = new Keyframe[NumFrames][];
                            Animations[AnimID].PosY = new Keyframe[NumFrames][];
                            Animations[AnimID].PosZ = new Keyframe[NumFrames][];

                            Animations[AnimID].RotX = new Keyframe[NumFrames][];
                            Animations[AnimID].RotY = new Keyframe[NumFrames][];
                            Animations[AnimID].RotZ = new Keyframe[NumFrames][];
                            Animations[AnimID].RotW = new Keyframe[NumFrames][];
                            for (int FrameID = 0; FrameID < NumFrames; FrameID++)
                            {
                                Animations[AnimID].PosX[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].PosY[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].PosZ[FrameID] = new Keyframe[NumBones];

                                Animations[AnimID].RotX[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotY[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotZ[FrameID] = new Keyframe[NumBones];
                                Animations[AnimID].RotW[FrameID] = new Keyframe[NumBones];
                            }

                            for (int boneID = 0; boneID < NumBones; boneID++)
                            {
                                AnimationBone AnimBone = AnimationBones.FirstOrDefault(x => x.Bone == boneID);

                                //frameIndex < 30 && studioAnimName == "@ak47_reload"
                                for (int frameID = 0; frameID < NumFrames; frameID++)
                                {
                                    //get current animation time (length) by divide frame index on "fps"
                                    float time = frameID / StudioAnim.fps;

                                    StudioBone StudioBone = MDL_StudioBones[boneID];
                                    //Transform bone = Bones[boneIndex];

                                    Vector3 Position = StudioBone.pos;
                                    Vector3 Rotation = StudioBone.rot;

                                    //BINGO! All animations are corrected :p
                                    if (AnimBone != null)
                                    {
                                        if ((AnimBone.Flags & (byte)ModelFlags.STUDIO_ANIM_RAWROT) > 0)
                                            Rotation = MathLibrary.ToEulerAngles(AnimBone.pQuat48);

                                        if ((AnimBone.Flags & (byte)ModelFlags.STUDIO_ANIM_RAWROT2) > 0)
                                            Rotation = MathLibrary.ToEulerAngles(AnimBone.pQuat64);

                                        if ((AnimBone.Flags & (byte)ModelFlags.STUDIO_ANIM_RAWPOS) > 0)
                                            Position = AnimBone.pVec48;

                                        if ((AnimBone.Flags & (byte)ModelFlags.STUDIO_ANIM_ANIMROT) > 0)
                                            Rotation += AnimBone.FrameAngles[(FramesLess && frameID != 0) ? frameID - 1 : frameID].Multiply(StudioBone.rotscale);

                                        if ((AnimBone.Flags & (byte)ModelFlags.STUDIO_ANIM_ANIMPOS) > 0)
                                            Position += AnimBone.FramePositions[(FramesLess && frameID != 0) ? frameID - 1 : frameID].Multiply(StudioBone.posscale);

                                        if ((AnimBone.Flags & (byte)ModelFlags.STUDIO_ANIM_DELTA) > 0)
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
                    MDL_SeqDescriptions = new StudioSeqDesc[MDL_Header.localseq_count];
                    Sequences = new SeqInfo[MDL_Header.localseq_count];

                    for (int seqID = 0; seqID < MDL_Header.localseq_count; seqID++)
                    {
                        int sequenceOffset = MDL_Header.localseq_offset + (212 * seqID);
                        reader.ReadType(ref MDL_SeqDescriptions[seqID], sequenceOffset);
                        StudioSeqDesc Sequence = MDL_SeqDescriptions[seqID];
                        Sequences[seqID] = new SeqInfo { name = reader.ReadNullTerminatedString(sequenceOffset + Sequence.szlabelindex), seq = Sequence };

                        reader.BaseStream.Position = sequenceOffset + Sequence.animindexindex;

                        var animID = reader.ReadShortArray(Sequence.groupsize[0] * Sequence.groupsize[1]);
                        //Debug.LogWarning(animIndices[0]);
                        // Just use the first animation for now
                        Sequences[seqID].ani = Animations[animID[0]];
                    }
                }

                //Materials
                MDL_TexturesInfo = new StudioTexture[MDL_Header.texture_count];
                MDL_Textures = new string[MDL_Header.texture_count];
                for (int texID = 0; texID < MDL_Header.texture_count; texID++)
                {
                    int textureOffset = MDL_Header.texture_offset + (64 * texID);
                    reader.ReadType(ref MDL_TexturesInfo[texID], textureOffset);
                    MDL_Textures[texID] = reader.ReadNullTerminatedString(textureOffset + MDL_TexturesInfo[texID].sznameindex);
                }

                int[] TDirOffsets = new int[MDL_Header.texturedir_count];
                MDL_TDirectories = new string[MDL_Header.texturedir_count];
                for (int dirID = 0; dirID < MDL_Header.texturedir_count; dirID++)
                {
                    reader.BaseStream.Position = MDL_Header.texturedir_offset + (4 * dirID);
                    TDirOffsets[dirID] = reader.ReadInt32();
                    MDL_TDirectories[dirID] = reader.ReadNullTerminatedString(TDirOffsets[dirID]).Replace("\\", "/");
                }
                //Materials

                //Bodyparts
                MDL_Bodyparts = new StudioBodyPart[MDL_Header.bodypart_count];
                for (int bodypartID = 0; bodypartID < MDL_Header.bodypart_count; bodypartID++)
                {
                    StudioBodyParts pBodypart = new StudioBodyParts();
                    int pBodypartOffset = MDL_Header.bodypart_offset + (16 * bodypartID);
                    reader.ReadType(ref pBodypart, pBodypartOffset);

                    MDL_Bodyparts[bodypartID].Name = reader.ReadNullTerminatedString(pBodypartOffset + pBodypart.sznameindex);
                    MDL_Bodyparts[bodypartID].Models = new Model[pBodypart.nummodels];

                    for (int modelID = 0; modelID < pBodypart.nummodels; modelID++)
                    {
                        StudioModel pModel = new StudioModel();
                        long pModelOffset = pBodypartOffset + (148 * modelID) + pBodypart.modelindex;
                        reader.ReadType(ref pModel, pModelOffset);

                        MDL_Bodyparts[bodypartID].Models[modelID].isBlank = (pModel.numvertices <= 0 || pModel.nummeshes <= 0);
                        MDL_Bodyparts[bodypartID].Models[modelID].model = pModel;
                        MDL_Bodyparts[bodypartID].Models[modelID].Meshes = new mstudiomesh_t[pModel.nummeshes];
                        for (int meshID = 0; meshID < pModel.nummeshes; meshID++)
                        {
                            mstudiomesh_t pMesh = new mstudiomesh_t();
                            long pMeshOffset = pModelOffset + (116 * meshID) + pModel.meshindex;
                            reader.ReadType(ref pMesh, pMeshOffset);

                            MDL_Bodyparts[bodypartID].Models[modelID].Meshes[meshID] = pMesh;
                        }

                        MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod = new Dictionary<int, List<int>>[8];

                        for (int i = 0; i < 8; i++)
                            MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod[i] = new Dictionary<int, List<int>>();

                        MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod = new StudioVertex[8][];
                    }
                }
                //BodyParts
            }

            if (physStream == null) return;

            using (UReader reader = new UReader(physStream))
            {
                phys = new PhysFile();
                phys.ReadToObject(reader);

                foreach (KeyValues.Entry entry in phys.keyValues["solid"])
                {
                    if (int.TryParse(entry["index"], out int index))
                    {
                        Solid solid = phys.solids[index];
                        if (float.TryParse(entry["mass"], out float mass))
                        {
                            solid.mass = mass;
                        }
                        solid.boneName = entry["name"];
                    }
                }
            }
        }
        public void SetIndices(int BodypartID, int ModelID, int LODID, int MeshID, List<int> Indices)
        {
            MDL_Bodyparts[BodypartID].Models[ModelID].IndicesPerLod[LODID].Add(MeshID, Indices);
        }
        public void SetVertices(int BodypartID, int ModelID, int LODID, int TotalVerts, int StartIndex, StudioVertex[] Vertexes)
        {
            MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID] = new StudioVertex[TotalVerts];
            Array.Copy(Vertexes, StartIndex, MDL_Bodyparts[BodypartID].Models[ModelID].VerticesPerLod[LODID], 0, TotalVerts);
        }
        static string HitTagType(int typeHit)
        {
            string returnType;
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
        public BoneWeight GetBoneWeight(StudioBoneWeight mBoneWeight)
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
    }
}