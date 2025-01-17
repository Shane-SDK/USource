using System.IO;
using System.Linq;
using System.Collections.Generic;
using USource.Converters;

namespace USource.SourceAsset
{
    public struct VmtAsset : ISourceAsset
    {
        public Location Location => location;
        Location location;
        public VmtAsset(Location loc)
        {
            location = loc;
        }
        public void GetDependencies(Stream stream, DependencyTree tree, bool recursive, ImportMode mode = ImportMode.CreateAndCache)
        {
            tree.Add(location);
            KeyValues keys = KeyValues.FromStream(stream);
            string shader = keys.Keys.First();
            KeyValues.Entry entry = keys[shader];
            ProcessKey("$basetexture");
            ProcessKey("$bumpmap");

            void ProcessKey(string key)
            {
                if (entry.ContainsKey(key))
                {
                    Location loc = new Location("materials/" + entry[key] + ".vtf", Location.Type.Source, tree.Root.location.ResourceProvider);
                    tree.Add(loc);
                }
            }
        }
    }
}