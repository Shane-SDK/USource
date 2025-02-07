namespace USource.Formats.VTX
{
    public struct ModelHeader : ISourceObject
    {
        public int numLODs;
        public int lodOffset;
        public void ReadToObject(UReader reader, int version = 0)
        {
            numLODs = reader.ReadInt32();
            lodOffset = reader.ReadInt32();
        }
    }
}