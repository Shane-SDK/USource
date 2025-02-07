namespace USource.Formats.VTX
{
    public struct MeshHeader : ISourceObject
    {
        public int numStripGroups;
        public int stripGroupHeaderOffset;
        public byte flags;

        public void ReadToObject(UReader reader, int version = 0)
        {
            numStripGroups = reader.ReadInt32();
            stripGroupHeaderOffset = reader.ReadInt32();
            flags = reader.ReadByte();
        }
    }
}