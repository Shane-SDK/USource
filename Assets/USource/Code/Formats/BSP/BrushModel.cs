using UnityEngine;

namespace USource.Formats.BSP
{
    public struct BrushModel : ISourceObject
    {
        public Vector3 min, max;  // bounding box
        public Vector3 origin;  // for sounds or lights
        public int headNode;    // index into node array
        public int firstFace, faceCount; // index into face array

        public void ReadToObject(UReader reader, int version = 0)
        {
            min = reader.ReadVector3();
            max = reader.ReadVector3();

            origin = reader.ReadVector3();

            headNode = reader.ReadInt32();
            firstFace = reader.ReadInt32();
            faceCount = reader.ReadInt32();
        }
    }
}
