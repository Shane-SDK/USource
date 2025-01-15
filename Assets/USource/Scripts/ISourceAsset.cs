using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
#endif

namespace USource
{
    public interface ISourceAsset
    {
        public void GetDependencies(Stream stream, List<Location> dependencies);
        public Location Location { get; }
        public static ISourceAsset FromLocation(Location location)
        {
            switch (location.GetAssetType())
            {
                case AssetType.None: return null;
                case AssetType.Mdl: return new MdlAsset(location);
                case AssetType.Vmt: return new VmtAsset(location);
                case AssetType.Vtf: return new VtfAsset(location);
            }

            return null;
        }
    }
}