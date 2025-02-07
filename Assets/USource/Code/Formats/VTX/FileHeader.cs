namespace USource.Formats.VTX
{
    public struct FileHeader : ISourceObject
    {
        public int version;

        public int vertCacheSize;
        public ushort maxBonesPerStrip;
        public ushort maxBonesPerFace;
        public int maxBonesPerVert;

        public int checkSum;

        public int numLODs;

        public int materialReplacementListOffset;

        public int numBodyParts;
        public int bodyPartOffset;

        public void ReadToObject(UReader reader, int version = 0)
        {
            this.version = reader.ReadInt32();
            vertCacheSize = reader.ReadInt32();
            maxBonesPerStrip = reader.ReadUInt16();
            maxBonesPerFace = reader.ReadUInt16();
            maxBonesPerVert = reader.ReadInt32();
            checkSum = reader.ReadInt32();
            numLODs = reader.ReadInt32();
            materialReplacementListOffset = reader.ReadInt32();
            numBodyParts = reader.ReadInt32();
            bodyPartOffset = reader.ReadInt32();
        }
    }
}