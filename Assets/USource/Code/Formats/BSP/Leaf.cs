using UnityEngine;
using USource.Converters;

namespace USource.Formats.BSP
{
    public struct Leaf : ISourceObject
    {
        public int contents;
        public short cluster;
        public short areaFlags;
        public short minX;
        public short minY;
        public short minZ;
        public short maxX;
        public short maxY;
        public short maxZ;
        public ushort firstLeafFace;
        public ushort leafFaceCount;
        public ushort firstLeafBrush;
        public ushort leafBrushCount;
        public short leafWaterDataID;
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
        public bool Contains(Vector3 c)
        {
            Vector3 min = TransformMin();
            Vector3 max = TransformMax();
            Bounds bounds = new Bounds { min = min - Vector3.one * 0.01f, max = max + Vector3.one * 0.01f };
            return bounds.Contains(c);
        }
        public void Draw(Color color)
        {
            Bounds bounds = new Bounds { min = TransformMin(), max = TransformMax() };
            Conversions.DrawBox(bounds.center, Quaternion.identity, bounds.size, color, 10.0f);
        }

        public void ReadToObject(UReader reader, int version = 0)
        {
            contents = reader.ReadInt32();
            cluster = reader.ReadInt16();
            areaFlags = reader.ReadInt16();
            minX = reader.ReadInt16();
            minY = reader.ReadInt16();
            minZ = reader.ReadInt16();
            maxX = reader.ReadInt16();
            maxY = reader.ReadInt16();
            maxZ = reader.ReadInt16();
            firstLeafFace = reader.ReadUInt16();
            leafFaceCount = reader.ReadUInt16();
            firstLeafBrush = reader.ReadUInt16();
            leafBrushCount = reader.ReadUInt16();
            leafWaterDataID = reader.ReadInt16();
            padding = reader.ReadInt16();
        }
    }
}
