namespace USource.Formats.VVD
{
    public struct VertexFileFixup : ISourceObject
    {
        public int lod;
        public int sourceVertexID;
        public int numVertexes;
        public void ReadToObject(UReader reader, int version = 0)
        {
            lod = reader.ReadInt32();
            sourceVertexID = reader.ReadInt32();
            numVertexes = reader.ReadInt32();
        }
    }
}