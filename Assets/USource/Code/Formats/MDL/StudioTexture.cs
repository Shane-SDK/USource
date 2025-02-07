using System.Runtime.InteropServices;

namespace USource.Formats.MDL
{
    public struct StudioTexture : ISourceObject
    {
        public int sznameindex;
        public int flags;
        public int used;
        public int unused1;
        public int material;
        public int clientmaterial;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public int[] unused;

        public void ReadToObject(UReader reader, int version = 0)
        {
            sznameindex = reader.ReadInt32();
            flags = reader.ReadInt32();
            used = reader.ReadInt32();
            reader.Skip(4);  // unused int
            material = reader.ReadInt32();
            clientmaterial = reader.ReadInt32();
            reader.Skip(4 * 10);  // unused int array of size 10
        }
    }
}
