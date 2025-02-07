namespace USource.Formats.BSP
{
    public struct Header : ISourceObject
    {
        public int identity;  // BSP file identifier
        public int version;  // BSP file version
        public LumpHeader[] lumps;
        public int mapRevision;  // the map's revision (iteration, version) number

        public void ReadToObject(UReader reader, int version = 0)
        {
            identity = reader.ReadInt32();
            this.version = reader.ReadInt32();

            lumps = new LumpHeader[64];
            reader.ReadSourceObjectArray(ref lumps, version);

            mapRevision = reader.ReadInt32();
        }
    }
}
