using System.IO;
using System.Collections.Generic;

namespace USource.SourceAsset
{
    public struct VtfAsset : ISourceAsset
    {
        public Location Location => location;
        Location location;
        public VtfAsset(Location loc)
        {
            location = loc;
        }
        public void GetDependencies(Stream stream, List<Location> depdendencies)
        {
            depdendencies.Add(location);
        }
    }
}