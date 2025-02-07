using UnityEngine;

namespace USource.Formats.BSP
{
    public struct Plane : ISourceObject
    {
        public Vector3 normal;
        public float distance;
        public int type;

        public void ReadToObject(UReader reader, int version = 0)
        {
            normal = reader.ReadVector3();
            distance = reader.ReadSingle();
            type = reader.ReadInt32();
        }
    }
}
