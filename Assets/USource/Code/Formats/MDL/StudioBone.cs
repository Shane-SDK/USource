using UnityEngine;

namespace USource.Formats.MDL
{
    public struct StudioBone : ISourceObject
    {
        public int sznameindex;
        public int parent;

        public int[] bonecontroller;  // 6

        public Vector3 pos;
        public Quaternion quat;
        public Vector3 rot;

        public Vector3 posscale;
        public Vector3 rotscale;

        public float[] poseToBone;  // 12

        public Quaternion qAlignment;
        public int flags;
        public int proctype;
        public int procindex;
        public int physicsbone;
        public int surfacepropidx;
        public int contents;

        public int[] unused;  // 8

        public void ReadToObject(UReader reader, int version = 0)
        {
            sznameindex = reader.ReadInt32();
            parent = reader.ReadInt32();

            bonecontroller = reader.ReadIntArray(6);

            pos = reader.ReadVector3();
            quat = reader.ReadQuaternion();
            rot = reader.ReadVector3();

            posscale = reader.ReadVector3();
            rotscale = reader.ReadVector3();

            poseToBone = reader.ReadSingleArray(12);

            qAlignment = reader.ReadQuaternion();
            flags = reader.ReadInt32();
            proctype = reader.ReadInt32();
            procindex = reader.ReadInt32();
            physicsbone = reader.ReadInt32();
            surfacepropidx = reader.ReadInt32();
            contents = reader.ReadInt32();

            reader.Skip(4 * 8);  // unused int array size of 8
        }
    }
}
