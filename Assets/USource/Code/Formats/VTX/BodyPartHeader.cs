namespace USource.Formats.VTX
{
    public struct BodyPartHeader : ISourceObject
    {
        public int numModels;
        public int modelOffset;

        public void ReadToObject(UReader reader, int version = 0)
        {
            numModels = reader.ReadInt32();
            modelOffset = reader.ReadInt32();
        }
    }
}