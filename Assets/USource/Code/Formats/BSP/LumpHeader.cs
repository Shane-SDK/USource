namespace USource.Formats.BSP
{
    public struct LumpHeader : ISourceObject
    {
        public int fileOffset;
        // offset into file (bytes)
        public int fileLength;
        // length of lump (bytes)
        public int version;
        // lump format version
        public int code;
        // lump ident code

        public void ReadToObject(UReader reader, int version = 0)
        {
            fileOffset = reader.ReadInt32();
            fileLength = reader.ReadInt32();
            this.version = reader.ReadInt32();
            code = reader.ReadInt32();


        }
    }
}
