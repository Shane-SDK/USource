namespace USource.Formats.BSP
{
    public struct LeafAmbientIndex : ISourceObject
    {
        public ushort ambientSampleCount;
        public ushort firstAmbientSample;

        public void ReadToObject(UReader reader, int version = 0)
        {
            ambientSampleCount = reader.ReadUInt16();
            firstAmbientSample = reader.ReadUInt16();
        }
    }
}
