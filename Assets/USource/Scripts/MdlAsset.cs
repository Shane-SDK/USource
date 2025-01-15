using System;
using System.IO;
using System.Collections.Generic;
using USource.Formats.Source.MDL;
using static USource.Formats.Source.MDL.StudioStruct;
#if UNITY_EDITOR
#endif

namespace USource
{
    public struct MdlAsset : ISourceAsset
    {
        public Location Location => location;
        Location location;
        public MdlAsset(Location loc)
        {
            location = loc;
        }
        public void GetDependencies(Stream stream, List<Location> depdendencies)
        {
            depdendencies.Add(location);
            depdendencies.Add(new Location(location.SourcePath.Replace(".mdl", ".vvd"), Location.Type.Source, location.ResourceProvider));
            depdendencies.Add(new Location(location.SourcePath.Replace(".mdl", ".vtx"), Location.Type.Source, location.ResourceProvider));
            depdendencies.Add(new Location(location.SourcePath.Replace(".mdl", ".sw.vtx"), Location.Type.Source, location.ResourceProvider));
            depdendencies.Add(new Location(location.SourcePath.Replace(".mdl", ".phy"), Location.Type.Source, location.ResourceProvider));
            UReader reader = new UReader(stream);
            StudioStruct.studiohdr_t header = default;
            reader.ReadType(ref header);

            mstudiotexture_t[] textures = new mstudiotexture_t[header.texture_count];
            string[] textureNames = new String[header.texture_count];
            for (Int32 texID = 0; texID < header.texture_count; texID++)
            {
                Int32 textureOffset = header.texture_offset + (64 * texID);
                reader.ReadType(ref textures[texID], textureOffset);
                textureNames[texID] = reader.ReadNullTerminatedString(textureOffset + textures[texID].sznameindex);
            }

            Int32[] TDirOffsets = new Int32[header.texturedir_count];
            string[] directoryNames = new String[header.texturedir_count];
            for (Int32 dirID = 0; dirID < header.texturedir_count; dirID++)
            {
                reader.ReadType(ref TDirOffsets[dirID], header.texturedir_offset + (4 * dirID));
                directoryNames[dirID] = reader.ReadNullTerminatedString(TDirOffsets[dirID]).Replace("\\", "/");
            }

            foreach (string dir in directoryNames)
            {
                foreach (string name in textureNames)
                {
                    Location location = new Location($"materials/{dir}{name}.vmt", Location.Type.Source, depdendencies[0].ResourceProvider);
                    if (location.ResourceProvider.ContainsFile(location.SourcePath))
                    {
                        Stream depStream = location.ResourceProvider[location];
                        new VmtAsset(location).GetDependencies(depStream, depdendencies);
                    }
                }
            }
        }
    }
}