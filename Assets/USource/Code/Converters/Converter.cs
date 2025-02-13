
using System;
using System.IO;
using UnityEngine;

namespace USource.Converters
{
    public interface IConverter
    {
        public const float uvScaleFactor = 1.25f;
        public const float physicsScalingFactor = 1.016f;
        public UnityEngine.Object CreateAsset(ImportContext ctx);
        public static IConverter FromLocation(Location location, System.IO.Stream assetStream)
        {
            IConverter converter;
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

                    converter = new Converters.ModelConverter(assetStream, vvdStream, vtxStream, physStream, ModelConverter.ImportOptions.Geometry);

                    physStream?.Close();
                    vvdStream?.Close();
                    vtxStream?.Close();
                    break;
                case ".vmt":
                    converter = new Converters.MaterialConverter(assetStream);
                    break;
                case ".vtf":
                    converter = new Converters.TextureConverter(assetStream, default);
                    break;
#if RealtimeCSG
                case ".vmf":
                    converter = new VmfConverter(assetStream);
                    break;
#endif
                default:
                    converter = null;
                    break;
            }

            return converter;
        }
        public static Vector3 SourceTransformPoint(Vector3 c)
        {
            return USource.settings.sourceToUnityScale * new Vector3(c.x, c.z, c.y);
        }
        public static Vector3 SourceTransformDirection(Vector3 c)
        {
            return new Vector3(c.x, c.z, c.y);
        }
        public static Vector3 SourceTransformAngles(Vector3 sourceAngles)
        {
            //sourceAngles = AxisConvertSource(sourceAngles);
            return (
                Quaternion.AngleAxis(-sourceAngles[1], Vector3.up) *
                Quaternion.AngleAxis(-sourceAngles[0], Vector3.forward) *
                Quaternion.AngleAxis(-sourceAngles[2], Vector3.right)
                ).eulerAngles;
        }
        public static Vector3 AxisConvertSource(Vector3 sourceAxis) => new Vector3(sourceAxis.x, sourceAxis.z, sourceAxis.y);
        public static Vector3 TransformPointSourcePhysicsToUnity(Vector3 sourcePhysics)
        {
            return new Vector3(sourcePhysics.x, -sourcePhysics.y, sourcePhysics.z) / physicsScalingFactor;
        }
        public static string ByteArrayToString(byte[] bytes)
        {
            int nullIndex = -1;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (((char)bytes[i]) == '\0')
                {
                    nullIndex = i;
                    break;
                }
            }
            return System.Text.Encoding.ASCII.GetString(bytes, 0, nullIndex);
        }
    }
    public enum ImportMode
    {
        CreateAndCache,
        AssetDatabase
    }
}
