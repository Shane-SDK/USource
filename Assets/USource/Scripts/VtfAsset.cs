using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
#endif

namespace USource
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