using System;
using UnityEngine;
using System.IO;

namespace USource.Formats.Source.PHYS
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
    public struct ConvexSolidHeader : ISourceObject
    {
        public int vertexOffset;
        public int boneIndex;
        public int flags;
        public int triangleCount;

        public void ReadToObject(UReader reader, int version = 0)
        {
            vertexOffset = reader.ReadInt32();
            boneIndex = reader.ReadInt32();
            flags = reader.ReadInt32();
            triangleCount = reader.ReadInt32();
        }
    }
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
    public class PhysFile : ISourceObject
    {
        public PhysFileHeader header;
        public Solid[] solids;
        public KeyValues keyValues;
        public void ReadToObject(UReader reader, int version = 0)
        {
            header.ReadToObject(reader);

            solids = new Solid[header.solidCount];
            for (int i = 0; i < header.solidCount; i++)
            {
                solids[i] = new PHYS.Solid();
                solids[i].ReadToObject(reader, 1);
            }

            // key data
            keyValues = KeyValues.Parse(System.Text.Encoding.ASCII.GetString(reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position))));
        }
    }
}
