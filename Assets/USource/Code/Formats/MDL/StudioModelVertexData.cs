namespace USource.Formats.MDL
{
    public struct StudioModelVertexData : ISourceObject
    {
        public int vertexData;
        public int tangentData;

        public void ReadToObject(UReader reader, int version = 0)
        {
            version = reader.ReadInt32();
            vertexData = reader.ReadInt32();
        }
    }
}
