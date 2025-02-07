namespace USource.Formats.PHYS
{
    public class PhysFile : ISourceObject
    {
        public PhysFileHeader header;
        public Solid[] solids;
        public KeyValues keyValues;
        public void ReadToObject(UReader reader, int version = 0)
        {
            header.ReadToObject(reader);

            solids = new Solid[header.solidCount];
            for (int i = 0; i < header.solidCount; i++)
            {
                solids[i] = new Solid();
                solids[i].ReadToObject(reader, 1);
            }

            // key data
            keyValues = KeyValues.Parse(System.Text.Encoding.ASCII.GetString(reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position))));
        }
    }
}
