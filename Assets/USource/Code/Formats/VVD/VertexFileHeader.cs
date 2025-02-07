namespace USource.Formats.VVD
{
    public struct VertexFileHeader : ISourceObject
    {
        public int id;
        public int version;
        public int checksum;
        public int numLODs;
        public int[] numLODVertexes;
        public int numFixups;
        public int fixupTableStart;
        public int vertexDataStart;
        public int tangentDataStart;

        public void ReadToObject(UReader reader, int version = 0)
        {
            id = reader.ReadInt32();
            this.version = reader.ReadInt32();
            checksum = reader.ReadInt32();
            numLODs = reader.ReadInt32();
            numLODVertexes = reader.ReadIntArray(8);
            numFixups = reader.ReadInt32();
            fixupTableStart = reader.ReadInt32();
            vertexDataStart = reader.ReadInt32();
            tangentDataStart = reader.ReadInt32();
        }
    }
}