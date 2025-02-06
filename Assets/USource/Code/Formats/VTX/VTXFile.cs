using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using USource.Formats.VVD;
using USource.Formats.MDL;

namespace USource.Formats.VTX
{
    public class VTXFile
    {
        public FileHeader VTX_Header;
        public VTXFile(Stream FileInput, MDLFile StudioMDL, VVDFile StudioVVD)
        {
            using (var reader = new UReader(FileInput))
            {
                StudioHeader MDL_Header = StudioMDL.MDL_Header;
                VTX_Header = reader.ReadSourceObject<FileHeader>();

                if (VTX_Header.checkSum != MDL_Header.checksum)
                    throw new FileLoadException(string.Format("{0}: Does not match the checksum in the .mdl", MDL_Header.name));

                int[] vertexoffset = new int[8];
                for (int bodypartID = 0; bodypartID < MDL_Header.bodypart_count; bodypartID++)
                {
                    BodyPartHeader pBodypart = new BodyPartHeader();
                    long pBodypartOffset = VTX_Header.bodyPartOffset + 8 * bodypartID;
                    reader.BaseStream.Position = pBodypartOffset;
                    pBodypart = reader.ReadSourceObject<BodyPartHeader>();

                    StudioBodyPart MDLPart = StudioMDL.MDL_Bodyparts[bodypartID];

                    for (int modelID = 0; modelID < pBodypart.numModels; modelID++)
                    {
                        Model MDLModel = MDLPart.Models[modelID];

                        if (MDLModel.isBlank)
                        {
                            //Debug.Log(String.Format("Model ID - {0} in bodypart \"{1}\" is blank, skip", modelID, MDLPart.Name));
                            continue;
                        }

                        ModelHeader pModel = new ModelHeader();
                        long pModelOffset = pBodypartOffset + 8 * modelID + pBodypart.modelOffset;
                        reader.BaseStream.Position = pModelOffset;
                        pModel = reader.ReadSourceObject<ModelHeader>();

                        //TODO: Fix all lod's per model to use other lod's than 1 (VVD / MDL)
                        for (int LODID = 0; LODID < 1; LODID++)
                        {
                            ModelLODHeader_t pLOD = new ModelLODHeader_t();
                            long pLODOffset = pModelOffset + 12 * LODID + pModel.lodOffset;
                            reader.ReadType(ref pLOD, pLODOffset);

                            //Temp remember verts count per lod model
                            int TotalVerts = 0;
                            for (int MeshID = 0; MeshID < MDLModel.model.nummeshes; MeshID++)
                            {
                                mstudiomesh_t MDLMesh = MDLPart.Models[modelID].Meshes[MeshID];

                                TotalVerts += MDLModel.Meshes[MeshID].VertexData.numlodvertices[LODID];

                                MeshHeader pMesh = new MeshHeader();
                                long pMeshOffset = pLODOffset + 9 * MeshID + pLOD.meshOffset;
                                reader.ReadType(ref pMesh, pMeshOffset);

                                List<int> pIndices = new List<int>();
                                for (int stripgroupID = 0; stripgroupID < pMesh.numStripGroups; stripgroupID++)
                                {
                                    StripGroupHeader pStripGroup = new StripGroupHeader();
                                    long pStripGroupOffset = pMeshOffset + 25 * stripgroupID + pMesh.stripGroupHeaderOffset;
                                    reader.ReadType(ref pStripGroup, pStripGroupOffset);

                                    Vertex[] Vertexes = new Vertex[pStripGroup.numVerts];
                                    reader.BaseStream.Position = pStripGroupOffset + pStripGroup.vertOffset;
                                    reader.ReadArray(ref Vertexes);

                                    reader.BaseStream.Position = pStripGroupOffset + pStripGroup.indexOffset;
                                    short[] Indices = reader.ReadShortArray(pStripGroup.numIndices);

                                    for (int stripID = 0; stripID < pStripGroup.numStrips; stripID++)
                                    {
                                        StripHeader VTXStrip = new StripHeader();
                                        long VTXStripOffset = pStripGroupOffset + 27 * stripID + pStripGroup.stripOffset;
                                        reader.ReadType(ref VTXStrip, VTXStripOffset);

                                        if ((VTXStrip.flags & (byte)ModelFlags.VTXStripGroupTriListFlag) > 0)
                                        {
                                            for (var j = VTXStrip.indexOffset; j < VTXStrip.indexOffset + VTXStrip.numIndices; j++)
                                            {
                                                pIndices.Add(Vertexes[Indices[j]].origMeshVertId + MDLMesh.vertexoffset);// + vertexoffset);
                                            }
                                        }
                                        else if ((VTXStrip.flags & (byte)ModelFlags.VTXStripGroupTriStripFlag) > 0)
                                        {
                                            for (var j = VTXStrip.indexOffset; j < VTXStrip.indexOffset + VTXStrip.numIndices - 2; j++)
                                            {
                                                var add = j % 2 == 1 ? new[] { j + 1, j, j + 2 } : new[] { j, j + 1, j + 2 };
                                                foreach (var idx in add)
                                                {
                                                    pIndices.Add(Vertexes[Indices[idx]].origMeshVertId + MDLMesh.vertexoffset);// + vertexoffset);
                                                }
                                            }
                                        }
                                    }
                                }

                                StudioMDL.SetIndices(bodypartID, modelID, LODID, MeshID, pIndices);
                                //StudioMDL.MDL_Bodyparts[bodypartID].Models[modelID].IndicesPerLod[LODID].Add(MeshID, pIndices);
                            }

                            //StudioMDL.MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod[LODID] = new mstudiovertex_t[TotalVerts];
                            //Array.Copy(StudioVVD.VVD_Vertexes[LODID], vertexoffset[LODID], StudioMDL.MDL_Bodyparts[bodypartID].Models[modelID].VerticesPerLod[LODID], 0, TotalVerts);

                            StudioMDL.SetVertices(bodypartID, modelID, LODID, TotalVerts, vertexoffset[LODID], StudioVVD.VVD_Vertexes[LODID]);

                            vertexoffset[LODID] += TotalVerts;
                        }
                    }
                }
            }
        }
        public struct FileHeader : ISourceObject
        {
            public int version;

            public int vertCacheSize;
            public ushort maxBonesPerStrip;
            public ushort maxBonesPerFace;
            public int maxBonesPerVert;

            public int checkSum;

            public int numLODs;

            public int materialReplacementListOffset;

            public int numBodyParts;
            public int bodyPartOffset;

            public void ReadToObject(UReader reader, int version = 0)
            {
                this.version = reader.ReadInt32();
                vertCacheSize = reader.ReadInt32();
                maxBonesPerStrip = reader.ReadUInt16();
                maxBonesPerFace = reader.ReadUInt16();
                maxBonesPerVert = reader.ReadInt32();
                checkSum = reader.ReadInt32();
                numLODs = reader.ReadInt32();
                materialReplacementListOffset = reader.ReadInt32();
                numBodyParts = reader.ReadInt32();
                bodyPartOffset = reader.ReadInt32();
            }
        }
        public struct BodyPartHeader : ISourceObject
        {
            public int numModels;
            public int modelOffset;

            public void ReadToObject(UReader reader, int version = 0)
            {
                numModels = reader.ReadInt32();
                modelOffset = reader.ReadInt32();
            }
        }
        public struct ModelHeader : ISourceObject
        {
            public int numLODs;
            public int lodOffset;
            public void ReadToObject(UReader reader, int version = 0)
            {
                numLODs = reader.ReadInt32();
                lodOffset = reader.ReadInt32();
            }
        }
        public struct ModelLODHeader_t : ISourceObject
        {
            public int numMeshes;
            public int meshOffset;
            public float switchPoint;

            public void ReadToObject(UReader reader, int version = 0)
            {
                numMeshes = reader.ReadInt32();
                meshOffset = reader.ReadInt32();
                switchPoint = reader.ReadSingle();
            }
        }
        public struct MeshHeader : ISourceObject
        {
            public int numStripGroups;
            public int stripGroupHeaderOffset;
            public byte flags;

            public void ReadToObject(UReader reader, int version = 0)
            {
                numStripGroups = reader.ReadInt32();
                stripGroupHeaderOffset = reader.ReadInt32();
                flags = reader.ReadByte();
            }
        }
        public struct StripGroupHeader : ISourceObject
        {
            public int numVerts;
            public int vertOffset;

            public int numIndices;
            public int indexOffset;

            public int numStrips;
            public int stripOffset;

            public byte flags;

            public void ReadToObject(UReader reader, int version = 0)
            {
                numVerts = reader.ReadInt32();
                vertOffset = reader.ReadInt32();
                numIndices = reader.ReadInt32();
                indexOffset = reader.ReadInt32();
                numStrips = reader.ReadInt32();
                stripOffset = reader.ReadInt32();
                flags = reader.ReadByte();
            }

            //TODO: Some custom engines / games has this bytes, like a Alien Swarm / CSGO / DOTA2 (except L4D and L4D2?)
            //public Int32 numTopologyIndices;
            //public Int32 topologyOffset;
        }
        public struct StripHeader : ISourceObject
        {
            // indexOffset offsets into the mesh's index array.
            public int numIndices;
            public int indexOffset;

            // vertexOffset offsets into the mesh's vert array.
            public int numVerts;
            public int vertOffset;

            // use this to enable/disable skinning.  
            // May decide (in optimize.cpp) to put all with 1 bone in a different strip 
            // than those that need skinning.
            public short numBones;

            public byte flags;

            public int numBoneStateChanges;
            public int boneStateChangeOffset;

            public void ReadToObject(UReader reader, int version = 0)
            {
                numIndices = reader.ReadInt32();
                indexOffset = reader.ReadInt32();
                numVerts = reader.ReadInt32();
                vertOffset = reader.ReadInt32();
                numBones = reader.ReadInt16();
                flags = reader.ReadByte();
                numBoneStateChanges = reader.ReadInt32();
                boneStateChangeOffset = reader.ReadInt32();
            }

            //TODO: Some custom engines / games has this bytes, like a Alien Swarm / CSGO / DOTA2 (except L4D and L4D2?)
            // These go last on purpose!
            //public Int32 numTopologyIndices;
            //public Int32 topologyOffset;
        }
        public struct Vertex : ISourceObject
        {
            public byte[] boneWeightIndices;
            public byte numBones;
            public ushort origMeshVertId;
            public byte[] boneID;

            public void ReadToObject(UReader reader, int version = 0)
            {
                boneWeightIndices = reader.ReadBytes(3);
                numBones = reader.ReadByte();
                origMeshVertId = reader.ReadUInt16();

                boneID = reader.ReadBytes(3);
            }
        }
    }
}