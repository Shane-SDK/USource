using System;

namespace USource.Formats.PHYS
{
    public struct PhysFileHeader : ISourceObject
    {
        public int size;
        public int id;
        public int solidCount;
        public int checkSum;
        public void ReadToObject(UReader reader, int version = 0)
        {
            size = reader.ReadInt32();
            id = reader.ReadInt32();
            solidCount = reader.ReadInt32();
            checkSum = reader.ReadInt32();
        }
    }
}
