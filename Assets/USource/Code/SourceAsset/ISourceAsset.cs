using System.IO;
using System.Collections.Generic;
using USource.Converters;

namespace USource.SourceAsset
{
    public interface ISourceAsset
    {
        public void GetDependencies(Stream stream, List<Location> dependencies, ImportMode mode = ImportMode.CreateAndCache);
        public Location Location { get; }
        public static ISourceAsset FromLocation(Location location)
        {
            switch (location.GetAssetType())
            {
                case AssetType.None: return null;
                case AssetType.Mdl: return new MdlAsset(location);
                case AssetType.Vmt: return new VmtAsset(location);
                case AssetType.Vtf: return new VtfAsset(location);
                case AssetType.Vmf: return new VmfAsset(location);
            }

            return null;
        }
    }
}