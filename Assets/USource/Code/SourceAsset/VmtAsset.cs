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
        public void GetDependencies(Stream stream, List<Location> depdendencies, ImportMode mode = ImportMode.CreateAndCache)
        {
            depdendencies.Add(location);
            KeyValues keys = KeyValues.FromStream(stream);
            string shader = keys.Keys.First();
            KeyValues.Entry entry = keys[shader];
            if (entry.ContainsKey("$basetexture"))
            {
                depdendencies.Add(new Location("materials/" + entry["$basetexture"] + ".vtf", Location.Type.Source, depdendencies[0].ResourceProvider));
            }
        }
    }
}