using System.IO;
using static USource.Formats.Source.MDL.StudioStruct;
using USource.Converters;

namespace USource.SourceAsset
{
    public struct MdlAsset : ISourceAsset
    {
        public Location Location => location;
        Location location;
        public MdlAsset(Location loc)
        {
            location = loc;
        }
        public void GetDependencies(Stream stream, DependencyTree tree, bool recursive, ImportMode importMode = ImportMode.CreateAndCache)
        {
            tree.Add(location);
            tree.Add(new Location(location.SourcePath.Replace(".mdl", ".vvd"), Location.Type.Source, location.ResourceProvider));
            tree.Add(new Location(location.SourcePath.Replace(".mdl", ".vtx"), Location.Type.Source, location.ResourceProvider));
            tree.Add(new Location(location.SourcePath.Replace(".mdl", ".sw.vtx"), Location.Type.Source, location.ResourceProvider));
            tree.Add(new Location(location.SourcePath.Replace(".mdl", ".phy"), Location.Type.Source, location.ResourceProvider));
            stream.Position = 0;
            UReader reader = new UReader(stream);
            studiohdr_t header = default;
            reader.ReadType(ref header);

            mstudiotexture_t[] textures = new mstudiotexture_t[header.texture_count];
            string[] textureNames = new string[header.texture_count];
            for (int texID = 0; texID < header.texture_count; texID++)
            {
                int textureOffset = header.texture_offset + 64 * texID;
                reader.ReadType(ref textures[texID], textureOffset);
                textureNames[texID] = reader.ReadNullTerminatedString(textureOffset + textures[texID].sznameindex);
            }

            int[] TDirOffsets = new int[header.texturedir_count];
            string[] directoryNames = new string[header.texturedir_count];
            for (int dirID = 0; dirID < header.texturedir_count; dirID++)
            {
                reader.ReadType(ref TDirOffsets[dirID], header.texturedir_offset + 4 * dirID);
                directoryNames[dirID] = reader.ReadNullTerminatedString(TDirOffsets[dirID]).Replace("\\", "/");
            }

            foreach (string dir in directoryNames)
            {
                foreach (string name in textureNames)
                {
                    Location location = new Location($"materials/{dir}{name}.vmt", Location.Type.Source, tree.Root.location.ResourceProvider);
                    if (recursive && USource.ResourceManager.GetStream(location, out Stream depStream, importMode))
                    {
                        new VmtAsset(location).GetDependencies(depStream, tree, true, importMode);
                    }
                    else
                    {
                        tree.Add(location);
                    }
                }
            }
        }
    }
}