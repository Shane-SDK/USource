using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using USource.Converters;
using USource.Formats.Source.VBSP;
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

            HashSet<Location> importedLocations = new();
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
                // check if material uses %include
                Location materialLocation = new Location(materialPath, Location.Type.Source, Location.ResourceProvider);
                if (ISourceAsset.TryResolvePatchMaterial(materialLocation, out Location patchedMaterial))
                    materialLocation = patchedMaterial;
                ISourceAsset.AddDependency(materialLocation, tree, recursive, mode);
            }

            // Add static props
            reader.BaseStream.Seek(header.Lumps[35].FileOfs, SeekOrigin.Begin);
            dgamelump_t[] gameLumps = new dgamelump_t[reader.ReadInt32()];
            reader.ReadArray(ref gameLumps, header.Lumps[35].FileOfs + 4);

            for (int i = 0; i < gameLumps.Length; i++)
            {
                if (gameLumps[i].Id == 1936749168)  // Static prop dictionary
                {
                    reader.BaseStream.Seek(gameLumps[i].FileOfs, SeekOrigin.Begin);
                    int propCount = reader.ReadInt32();
                    for (Int32 j = 0; j < propCount; j++)
                    {
                        string propPath = new string(reader.ReadChars(128));

                        if (propPath.Contains('\0'))
                            propPath = propPath.Split('\0')[0];
                        importedLocations.Add(new Location(propPath, Location.Type.Source));
                        ISourceAsset.AddDependency(new Location(propPath, Location.Type.Source, Location.ResourceProvider), tree, recursive, mode);
                    }
                }
            }

            // Entities
            reader.BaseStream.Seek(header.Lumps[0].FileOfs, SeekOrigin.Begin);
            MatchCollection Matches = Regex.Matches(
                new(reader.ReadChars(header.Lumps[0].FileLen)),
                @"{[^}]*}", RegexOptions.IgnoreCase);

 
            int[] quoteIndexBuffer = new int[4];
            foreach (Match m in Matches)
            {
                string[] lines = m.Value.Trim('{', '}', ' ').Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) continue;

                foreach (string line in lines)
                {
                    int quoteCount = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '"')
                        {
                            quoteIndexBuffer[quoteCount] = i;
                            quoteCount++;
                            if (quoteCount >= 4)
                                break;
                        }  // Find quotes
                    }
                    if (quoteCount < 3) break;

                    string key = line.Substring(quoteIndexBuffer[0] + 1, (quoteIndexBuffer[1] - quoteIndexBuffer[0] - 1));
                    string value = line.Substring(quoteIndexBuffer[2] + 1, (quoteIndexBuffer[3] - quoteIndexBuffer[2] - 1));

                    if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) break;

                    Location location = new Location(value, Location.Type.Source);

                    if (!importedLocations.Contains(location) && key == "model" && value.Length > 0 && value[0] != '*')
                    {
                        importedLocations.Add(location);
                        ISourceAsset.AddDependency(new Location(value, Location.Type.Source), tree, recursive, mode);
                    }
                }
            }
        }
    }
}
