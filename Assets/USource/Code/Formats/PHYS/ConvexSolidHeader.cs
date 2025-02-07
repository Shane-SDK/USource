namespace USource.Formats.PHYS
{
    public struct ConvexSolidHeader : ISourceObject
    {
        public int vertexOffset;
        public int boneIndex;
        public int flags;
        public int triangleCount;

        public void ReadToObject(UReader reader, int version = 0)
        {
            vertexOffset = reader.ReadInt32();
            boneIndex = reader.ReadInt32();
            flags = reader.ReadInt32();
            triangleCount = reader.ReadInt32();
        }
    }
}
