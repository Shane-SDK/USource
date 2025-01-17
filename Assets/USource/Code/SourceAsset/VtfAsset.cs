using System.IO;
using System.Collections.Generic;
using USource.Converters;

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
        public void GetDependencies(Stream stream, DependencyTree depdendencies, bool recursive, ImportMode mode = ImportMode.CreateAndCache)
        {
            depdendencies.Add(location);
        }
    }
}