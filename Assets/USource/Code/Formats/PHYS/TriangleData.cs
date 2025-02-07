using System.IO;

namespace USource.Formats.PHYS
{
    public struct TriangleData : ISourceObject
    {
        public short v1;
        public short v2;
        public short v3;

        public void ReadToObject(UReader reader, int version = 0)
        {
            reader.BaseStream.Seek(1 + 1 + 2, SeekOrigin.Current);  // skip unknown values 
            v3 = reader.ReadInt16();
            reader.BaseStream.Seek(2, SeekOrigin.Current);  // skip unknown value
            v2 = reader.ReadInt16();
            reader.BaseStream.Seek(2, SeekOrigin.Current);  // skip unknown value
            v1 = reader.ReadInt16();
            reader.BaseStream.Seek(2, SeekOrigin.Current);  // skip unknown value
        }
    }
}
