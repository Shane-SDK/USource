namespace USource.Formats.MDL
{
    public struct StudioHitboxSet : ISourceObject
    {
        public int sznameindex;
        public int numhitboxes;
        public int hitboxindex;
        public void ReadToObject(UReader reader, int version = 0)
        {
            sznameindex = reader.ReadInt32();
            numhitboxes = reader.ReadInt32();
            hitboxindex = reader.ReadInt32();
        }
    }
}
