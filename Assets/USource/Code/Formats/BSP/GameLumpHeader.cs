namespace USource.Formats.BSP
{
    public struct GameLumpHeader : ISourceObject
    {
        public int id;  // gamelump ID
        public ushort flags;    // flags
        public ushort version;  // gamelump version
        public int fileOffset;  // offset to this gamelump
        public int fileLength;  // length

        public void ReadToObject(UReader reader, int version = 0)
        {
            id = reader.ReadInt32();
            flags = reader.ReadUInt16();
            this.version = reader.ReadUInt16();
            fileOffset = reader.ReadInt32();
            fileLength = reader.ReadInt32();
        }
    }
}
