using UnityEngine;
using Unity.Mathematics;
using USource.Converters;

namespace USource.Formats.BSP
{
    public struct Node : ISourceObject
    {
        public int plane;
        public int2 children;
        public short minX;
        public short minY;
        public short minZ;
        public short maxX;
        public short maxY;
        public short maxZ;
        public ushort firstFace;
        public ushort faceCount;
        public short area;
        public short padding;

        public Vector3 TransformMin()
        {
            Vector3 min = IConverter.SourceTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = IConverter.SourceTransformPoint(new Vector3(maxX, maxY, maxZ));
            return new Vector3(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Min(min.z, max.z));
        }
        public Vector3 TransformMax()
        {
            Vector3 min = IConverter.SourceTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = IConverter.SourceTransformPoint(new Vector3(maxX, maxY, maxZ));
            return new Vector3(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y), Mathf.Max(min.z, max.z));
        }
        public bool Contains(Vector3 c, float tolerance = 0.01f)
        {
            Vector3 min = TransformMin();
            Vector3 max = TransformMax();
            Bounds bounds = new Bounds { min = min - Vector3.one * tolerance, max = max + Vector3.one * tolerance };
            return bounds.Contains(c);
        }
        public void Draw(Color color)
        {
            Bounds bounds = new Bounds { min = TransformMin(), max = TransformMax() };
            Conversions.DrawBox(bounds.center, Quaternion.identity, bounds.size, color, 10.0f);
        }
        public void ReadToObject(UReader reader, int version = 0)
        {
            plane = reader.ReadInt32();
            children.x = reader.ReadInt32();
            children.y = reader.ReadInt32();
            minX = reader.ReadInt16();
            minY = reader.ReadInt16();
            minZ = reader.ReadInt16();
            maxX = reader.ReadInt16();
            maxY = reader.ReadInt16();
            maxZ = reader.ReadInt16();
            firstFace = reader.ReadUInt16();
            faceCount = reader.ReadUInt16();
            area = reader.ReadInt16();
            padding = reader.ReadInt16();
        }
    }
}
