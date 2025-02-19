using System.IO;
using System.Collections.Generic;
using USource.Converters;
using System.Linq;

namespace USource.SourceAsset
{
    public interface ISourceAsset
    {
        public void GetDependencies(Stream stream, DependencyTree tree, bool recursive, ImportMode mode = ImportMode.CreateAndCache);
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
                case AssetType.Bsp: return new BspAsset(location);
            }

            return null;
        }
        protected static void AddDependency(Location dependency, DependencyTree tree, bool recursive, ImportMode mode)
        {
            if (recursive && USource.ResourceManager.GetStream(dependency, out Stream depStream, mode))
            {
                ISourceAsset depAsset = ISourceAsset.FromLocation(dependency);
                depAsset.GetDependencies(depStream, tree, true, mode);
                depStream?.Close();
            }
            else
            {
                tree.Add(dependency);
            }
        }
        public static bool TryResolvePatchMaterial(Location materialLocation, out Location patchedMaterial)
        {
            patchedMaterial = default;
            if (USource.ResourceManager.GetStream(materialLocation, out Stream matStream, ImportMode.CreateAndCache))
            {
                KeyValues keys = KeyValues.FromStream(matStream);
                if (keys.First().Key == "patch" && keys.First().Value.ContainsKey("include"))
                {
                    // use include key material instead of this
                    patchedMaterial = new Location(keys.First().Value["include"], Location.Type.Source, materialLocation.ResourceProvider);
                    matStream?.Close();
                    return true;
                }
            }
            matStream?.Close();
            return false;
        }
    }
}