using UnityEngine;

namespace USource.Formats.MDL
{
    public struct StudioAttachment : ISourceObject
    {
        public int sznameindex;
        public ushort flags;
        public int localbone;
        public Matrix4x4 local;
        public void ReadToObject(UReader reader, int version = 0)
        {
            sznameindex = reader.ReadInt32();
            flags = reader.ReadUInt16();
            localbone = reader.ReadInt32();

            local = new Matrix4x4();
            local.SetRow(0, reader.ReadVector3());
            local.SetRow(1, reader.ReadVector3());
            local.SetRow(2, reader.ReadVector3());
            local.SetRow(3, reader.ReadVector3());

            reader.Skip(4 * 8);  // unused int32 array of size 8
        }
    }
}
