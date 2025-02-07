namespace USource.Formats.MDL
{
    public struct StudioMeshVertexData : ISourceObject
    {
        public int modelvertexdata;
        public int[] numlodvertices;
        public void ReadToObject(UReader reader, int version = 0)
        {
            modelvertexdata = reader.ReadInt32();
            numlodvertices = reader.ReadIntArray(8);
        }
    }
}
