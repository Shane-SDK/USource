using UnityEngine;
using System.Runtime.InteropServices;

namespace USource.Formats.MDL
{
    public struct StudioBBox : ISourceObject
    {
        public int bone;
        public int group;                 // intersection group
        public Vector3 bbmin;              // bounding box
        public Vector3 bbmax;
        public int szhitboxnameindex;  // offset to the name of the hitbox.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] unused;

        public void ReadToObject(UReader reader, int version = 0)
        {
            bone = reader.ReadInt32();
            group = reader.ReadInt32();
            bbmin = reader.ReadVector3();
            bbmax = reader.ReadVector3();
            szhitboxnameindex = reader.ReadInt32();
            reader.Skip(4 * 8);  // unused int array of size 8
        }
    }
}
