using UnityEngine;

namespace USource.Formats.PHYS
{
    public struct CompactSurfaceHeader : ISourceObject
    {
        public int size;
        public int vPhysicsId;
        public short version;
        public short modelType;
        public int surfaceSize;
        public Vector3 dragAxisAreas;
        public int axisMapSize;

        public void ReadToObject(UReader reader, int version = 0)
        {
            size = reader.ReadInt32();
            vPhysicsId = reader.ReadInt32();  // should be VPHY in ASCII
            this.version = reader.ReadInt16();
            modelType = reader.ReadInt16();
            surfaceSize = reader.ReadInt32();
            dragAxisAreas = reader.ReadVector3();
            axisMapSize = reader.ReadInt32();
        }
    }
}
