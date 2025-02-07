namespace USource.Formats.BSP
{
    public struct Edge : ISourceObject
    {
        public ushort index0;
        public ushort index1;

        public void ReadToObject(UReader reader, int version = 0)
        {
            index0 = reader.ReadUInt16();
            index1 = reader.ReadUInt16();
        }
    }
}
