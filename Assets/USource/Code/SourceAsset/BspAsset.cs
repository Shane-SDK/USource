using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USource.Converters;
using static USource.Formats.Source.VBSP.VBSPStruct;

namespace USource.SourceAsset
{
    public struct BspAsset : ISourceAsset
    {
        public Location Location => location;
        public Location location;
        public BspAsset(Location location)
        {
            this.location = location;
        }
        public void GetDependencies(Stream stream, DependencyTree tree, bool recursive, ImportMode mode = ImportMode.CreateAndCache)
        {
            // Get header
            // Add materials from texture lump
            // Add models from props/point entities
            // sky???


            UReader reader = new UReader(stream);
            dheader_t header = default;
            reader.ReadType(ref header);
            int textureCount = header.Lumps[44].FileLen / 4;
            int[] indexArray = new int[textureCount];
            reader.ReadArray(ref indexArray, header.Lumps[44].FileOfs);

            // iterate over each index in the texture-index table
            for (int i = 0; i < textureCount; i++)
            {
                string materialPath = $"materials/{reader.ReadNullTerminatedString(header.Lumps[43].FileOfs + indexArray[i]).ToLower()}.vmt";
                ISourceAsset.AddDependency(new Location(materialPath, Location.Type.Source, Location.ResourceProvider), tree, recursive, mode);
            }
        }
    }
}
