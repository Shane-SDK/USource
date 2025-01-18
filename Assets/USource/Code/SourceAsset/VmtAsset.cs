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

            void ProcessSkyMaterial(Location skyLocation)
            {
                if (recursive && USource.ResourceManager.GetStream(skyLocation, out Stream skyStream))
                {
                    new VmtAsset(skyLocation).GetDependencies(skyStream, tree, true);
                }
                else
                {
                    tree.Add(skyLocation);
                }
            }

            if (location.SourcePath.Contains("/skybox/") && location.SourcePath.Contains("ft.vmt"))
            {
                ProcessSkyMaterial(new Location(location.SourcePath.Replace("ft.vmt", "up.vmt"), Location.Type.Source, location.ResourceProvider));
                ProcessSkyMaterial(new Location(location.SourcePath.Replace("ft.vmt", "lf.vmt"), Location.Type.Source, location.ResourceProvider));
                ProcessSkyMaterial(new Location(location.SourcePath.Replace("ft.vmt", "rt.vmt"), Location.Type.Source, location.ResourceProvider));
                ProcessSkyMaterial(new Location(location.SourcePath.Replace("ft.vmt", "bk.vmt"), Location.Type.Source, location.ResourceProvider));
                ProcessSkyMaterial(new Location(location.SourcePath.Replace("ft.vmt", "dn.vmt"), Location.Type.Source, location.ResourceProvider));
            }

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