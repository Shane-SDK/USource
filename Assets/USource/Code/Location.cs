using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USource
{
    public struct Location : IEquatable<Location>
    {
        public enum Type
        {
            Source,
            AssetDatabase,
            Resource,
            Absolute
        }
        public readonly string SourcePath => this.sourcePath;
        public string AssetPath
        {
            get
            {
                return USource.SourcePathToAssetPath(ResolveExtension(sourcePath));
            }
        }
        public string AbsolutePath
        {
            get
            {
                return $"{UnityEngine.Application.dataPath}/{AssetPath}";
            }
        }
        public readonly IResourceProvider ResourceProvider => resourceProvider;
        public bool DataExists => resourceProvider != null && resourceProvider.ContainsFile(sourcePath);
        readonly string sourcePath;
        readonly IResourceProvider resourceProvider;
        public Location(string path, Type type, IResourceProvider provider = null)
        {
            path = path.ToLower().Replace('\\', '/');
            switch (type)
            {
                case Type.Source:
                    this.sourcePath = path; break;
                case Type.AssetDatabase:
                    this.sourcePath = USource.AssetPathToSourcePath(path); break;
                default:
                    this.sourcePath = path; break;
            }

            resourceProvider = provider;
        }
        public string ResolveExtension(string path)
        {
            int startIndex = path.IndexOf('.');
            if (startIndex == -1)
                return path;

            string extension = path.Substring(startIndex, path.Length - startIndex);
            if (extension == ".sw.vtx")
                return path.Remove(startIndex, path.Length - startIndex) + ".vtx~";
            else if (
                extension == ".vtx" ||
                extension == ".phy" ||
                extension == ".vvd")
                return path + '~';

            return path;
        }
        public bool Equals(Location other)
        {
            return other.GetHashCode().Equals(GetHashCode());
        }
        public override int GetHashCode()
        {
            if (resourceProvider == null)
                return sourcePath.GetHashCode();
            else
                return HashCode.Combine(sourcePath, resourceProvider.GetHashCode());
        }
        public AssetType GetAssetType()
        {
            int startIndex = sourcePath.IndexOf('.');
            if (startIndex == -1)
                return AssetType.None;

            string extension = sourcePath.Substring(startIndex, sourcePath.Length - startIndex);

            switch (extension)
            {
                case ".mdl": return AssetType.Mdl;
                case ".vmt": return AssetType.Vmt;
                case ".vtf": return AssetType.Vtf;
                case ".vmf": return AssetType.Vmf;
                case ".bsp": return AssetType.Bsp;
            }

            return AssetType.None;
        }
        public Location CopyNoResourceLocation()
        {
            return new Location(SourcePath, Type.Source, null);
        }
    }
}
