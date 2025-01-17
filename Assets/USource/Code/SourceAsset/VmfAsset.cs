using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using USource.Converters;
using VMFParser;

namespace USource.SourceAsset
{
    public struct VmfAsset : ISourceAsset
    {
        public Location Location => location;
        public Location location;
        public VmfAsset(Location location)
        {
            this.location = location;
        }
        public void GetDependencies(Stream stream, DependencyTree dependencies, bool recursive, ImportMode mode = ImportMode.CreateAndCache)
        {
            dependencies.Add(location);
            VMFParser.VMF vmf = new VMFParser.VMF(ReadLines(() => stream, Encoding.UTF8).ToArray());  
            
            HashSet<Location> dependencySet = new();

            foreach (VBlock block in vmf.Body.WhereClass<VBlock>())
            {
                if (block.TryGetValue("model", out string modelPath))
                {
                    Location location = new Location(modelPath, Location.Type.Source);
                    if (!dependencySet.Contains(location))
                    {
                        dependencySet.Add(location);
                        if (USource.ResourceManager.GetStream(location, out Stream depStream, mode))
                        {
                            new MdlAsset(location).GetDependencies(depStream, dependencies, recursive);
                        }
                    }
                }

                foreach (VBlock side in block.Body
                        .WhereClass<VBlock>()
                        .SelectMany(e => e.Body)
                        .WhereClass<VBlock>()
                        .Where(e => e.Name == "side"))
                {
                    // Get material
                    if (!side.TryGetValue("material", out string materialPath)) continue;
                    Location location = new Location($"materials/{materialPath}.vmt", Location.Type.Source);
                    if (!dependencySet.Contains(location))
                    {
                        dependencySet.Add(location);
                        if (USource.ResourceManager.GetStream(location, out Stream depStream, mode))
                        {
                            new VmtAsset(location).GetDependencies(depStream, dependencies, recursive);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// FUCK THIS GOOP
        /// </summary>
        /// <param name="streamProvider"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static IEnumerable<string> ReadLines(System.Func<Stream> streamProvider,
                                     Encoding encoding)
        {
            using (var stream = streamProvider())
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}
