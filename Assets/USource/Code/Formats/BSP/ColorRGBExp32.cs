namespace USource.Formats.BSP
{
    public struct ColorRGBExp32 : ISourceObject
    {
        public byte r;
        public byte g;
        public byte b;
        public sbyte exponent;
        public void ReadToObject(UReader reader, int version = 0)
        {
            r = reader.ReadByte();
            g = reader.ReadByte();
            b = reader.ReadByte();
            exponent = (sbyte)reader.ReadByte();
        }
    }
}
