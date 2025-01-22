
using System;
using System.IO;
using UnityEngine;

namespace USource.Converters
{
    public abstract class Converter
    {
        public const float uvScaleFactor = 1.25f;
        public const float worldSpaceScaleFactor = 0.025f;
        public UnityEngine.Object UnityObject
        {
            get
            {
                return unityObject;
            }
        }
        public readonly string sourcePath;
        public string AssetDatabasePath
        {
            get
            {
                return $"Assets/USource/Assets/{USource.StripExtension(sourcePath)}.{USource.GetUnityAssetExtension(UnityObjectType)}";
            }
        }
        public string AbsolutePath
        {
            get
            {
                return $"{Application.dataPath}/{AssetDatabasePath.Remove(0, 7)}";
            }
        }
        public Type UnityObjectType
        {
            get
            {
                return USource.GetTypeFromExtension(sourcePath.Split('.')[^1]);
            }
        }
        protected UnityEngine.Object unityObject;
        public Converter(string sourcePath, System.IO.Stream stream)
        {
            this.sourcePath = sourcePath;
        }
        public abstract UnityEngine.Object CreateAsset(ImportContext ctx);
        public static Converter FromLocation(Location location, System.IO.Stream assetStream)
        {
            Converter converter;
            string extension = System.IO.Path.GetExtension(location.SourcePath);

            if (location.ResourceProvider == null && USource.ResourceManager.TryFindResourceProvider(location, out IResourceProvider provider))
                location = new Location(location.SourcePath, Location.Type.Source, provider);

            switch (extension)
            {
                case ".mdl":

                    bool TryGetStream(string path, out Stream stream)
                    {
                        stream = null;
                        if (location.ResourceProvider.TryGetFile(path, out stream))
                            return true;

                        return false;
                    }

                    // Attempt to load triangle/vertex info

                    // VVD (vertices)
                    string vvdPath = location.SourcePath.Replace(".mdl", ".vvd");
                    TryGetStream(vvdPath, out Stream vvdStream);

                    // VTX (Triangles)
                    string vtxPath = location.SourcePath.Replace(".mdl", ".vtx");
                    if (TryGetStream(vtxPath, out Stream vtxStream) == false)
                        if (TryGetStream(vtxPath.Replace(".vtx", ".dx90.vtx"), out vtxStream) == false)
                            TryGetStream(vtxPath.Replace(".vtx", ".dx80.vtx"), out vtxStream);

                    // Physics model
                    TryGetStream(location.SourcePath.Replace(".mdl", ".phy"), out Stream physStream);

                    converter = new Converters.ModelConverter(location.SourcePath, assetStream, vvdStream, vtxStream, physStream, ModelConverter.ImportOptions.Geometry);

                    physStream?.Close();
                    vvdStream?.Close();
                    vtxStream?.Close();
                    break;
                case ".vmt":
                    converter = new Converters.MaterialConverter(location.SourcePath, assetStream);
                    break;
                case ".vtf":
                    converter = new Converters.TextureConverter(location.SourcePath, assetStream, default);
                    break;
                case ".vmf":
                    converter = new VmfConverter(location.SourcePath, assetStream);
                    break;
                default:
                    converter = null;
                    break;
            }

            return converter;
        }
        /// <summary>
        /// Transforms a point from Source to Unity coordinates
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Vector3 SourceTransformPoint(Vector3 c)
        {
            return worldSpaceScaleFactor * new Vector3(-c.x, c.z, -c.y);
        }
        public static Vector3 SourceTransformPointHammer(Vector3 c)
        {
            return worldSpaceScaleFactor * new Vector3(-c.y, c.z, c.x);
        }
        public static Vector3 SourceTransformDirection(Vector3 c)
        {
            return new Vector3(-c.x, c.z, -c.y);
        }
        public static Vector3 SourceTransformAngles(Vector3 sourceAngles)
        {
            //sourceAngles = AxisConvertSource(sourceAngles);
            return (
                Quaternion.AngleAxis(-sourceAngles[1], Vector3.up) *
                Quaternion.AngleAxis(sourceAngles[0], Vector3.forward) *
                Quaternion.AngleAxis(sourceAngles[2], Vector3.right)
                ).eulerAngles;
        }
        public static Vector3 SourceTransformAnglesHammer(Vector3 sourceAngles)
        {
            //sourceAngles = AxisConvertSource(sourceAngles);
            return (
                Quaternion.AngleAxis(-sourceAngles[1] + 90, Vector3.up) *
                Quaternion.AngleAxis(sourceAngles[0], Vector3.forward) *
                Quaternion.AngleAxis(sourceAngles[2], Vector3.right)
                ).eulerAngles;
        }
        public static Vector3 AxisConvertSource(Vector3 sourceAxis) => new Vector3(sourceAxis.x, sourceAxis.z, sourceAxis.y);

        
    }
    public enum ImportMode
    {
        CreateAndCache,
        AssetDatabase
    }
}
