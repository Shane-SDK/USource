using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using USource.Formats.MDL;

namespace USource.Formats.VVD
{
    public class VVDFile
    {
        public VertexFileHeader VVD_Header;
        public StudioVertex[][] VVD_Vertexes;
        public VertexFileFixup[] VVD_Fixups;
        public bool HasTangents;

        public VVDFile(Stream FileInput, MDLFile mdl)
        {
            using (var reader = new UReader(FileInput))
            {
                VVD_Header = reader.ReadSourceObject<VertexFileHeader>();

                if (VVD_Header.checksum != mdl.MDL_Header.checksum)
                    throw new FileLoadException(string.Format("{0}: Does not match the checksum in the .mdl", mdl.MDL_Header.name));

                if (VVD_Header.numFixups > 0)
                {
                    VVD_Fixups = new VertexFileFixup[VVD_Header.numFixups];
                    reader.ReadSourceObjectArray(ref VVD_Fixups);
                }

                //TODO
                HasTangents = VVD_Header.tangentDataStart != 0;

                //"HasTagents" used to avoid non-zero length
                var sizeVerts = (HasTangents ? VVD_Header.tangentDataStart - VVD_Header.vertexDataStart : reader.InputStream.Length - VVD_Header.vertexDataStart) / 48;
                var tempVerts = new StudioVertex[sizeVerts];
                reader.ReadSourceObjectArray(ref tempVerts);

                VVD_Vertexes = new StudioVertex[VVD_Header.numLODs][];
                var lodVerts = new List<StudioVertex>();

                for (var lodID = 0; lodID < VVD_Header.numLODs; ++lodID)
                {
                    if (VVD_Header.numFixups == 0)
                    {
                        VVD_Vertexes[lodID] = tempVerts.Take(VVD_Header.numLODVertexes[lodID]).ToArray();
                        for (int c = 0; c < VVD_Vertexes[lodID].Length; c++)
                            VVD_Vertexes[lodID][c].m_vecTexCoord.y = VVD_Vertexes[lodID][c].m_vecTexCoord.y;
                        continue;
                    }

                    lodVerts.Clear();

                    foreach (var vertexFixup in VVD_Fixups)
                    {
                        if (vertexFixup.lod >= lodID)
                        {
                            lodVerts.AddRange(tempVerts.Skip(vertexFixup.sourceVertexID).Take(vertexFixup.numVertexes));
                        }
                    }

                    VVD_Vertexes[lodID] = lodVerts.ToArray();
                    for (int c = 0; c < VVD_Vertexes[lodID].Length; c++)
                        VVD_Vertexes[lodID][c].m_vecTexCoord.y = VVD_Vertexes[lodID][c].m_vecTexCoord.y;
                }
            }
        }
    }
}