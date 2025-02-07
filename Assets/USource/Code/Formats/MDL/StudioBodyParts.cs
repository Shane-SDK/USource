namespace USource.Formats.MDL
{
    public struct StudioBodyParts : ISourceObject
    {
        public int sznameindex;
        public int nummodels;
        public int _base;
        public int modelindex;
        public void ReadToObject(UReader reader, int version = 0)
        {
            sznameindex = reader.ReadInt32();
            nummodels = reader.ReadInt32();
            _base = reader.ReadInt32();
            modelindex = reader.ReadInt32();
        }
    }
}
