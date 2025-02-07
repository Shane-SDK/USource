namespace USource.Formats.BSP
{
    public struct CompressedLightCube : ISourceObject
    {
        public ColorRGBExp32 color0, color1, color2, color3, color4, color5;

        public void ReadToObject(UReader reader, int version = 0)
        {
            color0.ReadToObject(reader, version);
            color1.ReadToObject(reader, version);
            color2.ReadToObject(reader, version);
            color3.ReadToObject(reader, version);
            color4.ReadToObject(reader, version);
            color5.ReadToObject(reader, version);
        }
    }
}
