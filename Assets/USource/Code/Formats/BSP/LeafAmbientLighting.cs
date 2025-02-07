namespace USource.Formats.BSP
{
    public struct LeafAmbientLighting : ISourceObject
    {
        public CompressedLightCube cube;
        public byte x;
        public byte y;
        public byte z;
        public byte pad;
        public void ReadToObject(UReader reader, int version = 0)
        {
            cube.ReadToObject(reader, version);
            x = reader.ReadByte();
            y = reader.ReadByte();
            z = reader.ReadByte();
            pad = reader.ReadByte();
        }
    }
}
