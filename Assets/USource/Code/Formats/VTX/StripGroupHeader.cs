namespace USource.Formats.VTX
{
    public struct StripGroupHeader : ISourceObject
    {
        public int numVerts;
        public int vertOffset;

        public int numIndices;
        public int indexOffset;

        public int numStrips;
        public int stripOffset;

        public byte flags;

        public void ReadToObject(UReader reader, int version = 0)
        {
            numVerts = reader.ReadInt32();
            vertOffset = reader.ReadInt32();
            numIndices = reader.ReadInt32();
            indexOffset = reader.ReadInt32();
            numStrips = reader.ReadInt32();
            stripOffset = reader.ReadInt32();
            flags = reader.ReadByte();
        }

        //TODO: Some custom engines / games has this bytes, like a Alien Swarm / CSGO / DOTA2 (except L4D and L4D2?)
        //public Int32 numTopologyIndices;
        //public Int32 topologyOffset;
    }
}