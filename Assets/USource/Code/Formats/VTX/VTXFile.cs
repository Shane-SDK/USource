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
                {
                    Debug.LogWarning($"{MDL_Header.name} checksum error {VTX_Header.checkSum} (VTX) != {MDL_Header.checksum} (MDL)");
                }

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
        
    }
}