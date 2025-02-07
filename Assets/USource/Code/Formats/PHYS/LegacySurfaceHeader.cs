using UnityEngine;
using System.IO;

namespace USource.Formats.PHYS
{
    public struct LegacySurfaceHeader : ISourceObject
    {
        public int size;
        public Vector3 centerMass;
        public Vector3 rotationInertia;
        public float upperLimitRadius;
        public byte maxDeviation;
        public int byteSize;
        public int ledgeTreeOffsetRoot;

        public void ReadToObject(UReader reader, int version = 0)
        {
            reader.BaseStream.Seek(48, SeekOrigin.Current);
            //size = reader.ReadInt32();
            //centerMass = reader.ReadVector3D();
            //rotationInertia = reader.ReadVector3D();
            //upperLimitRadius = reader.ReadSingle();
            //int packedInt = reader.ReadInt32();
            //maxDeviation = (byte)(packedInt >> 24);
            //byteSize = (packedInt & 0xFFFFFF);
            //ledgeTreeOffsetRoot = reader.ReadInt32();
            //reader.BaseStream.Seek(3 * 4, SeekOrigin.Current);  // skip dummy bytes (dummy[2] is "IVPS" or 0)
            //Debug.Log(reader.BaseStream.Position.ToString("X"));
        }
    }
}
