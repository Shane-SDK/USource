using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using USource.Converters;

namespace USource.Formats.Source.PHYS
{
    public struct PhysFileHeader : ISourceObject
    {
        public int size;
        public int id;
        public int solidCount;
        public long checkSum;

        public void ReadToObject(UReader reader, int version = 0)
        {
            throw new NotImplementedException();
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
            dragAxisAreas = reader.ReadVector3D();
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
    public class CollisionData : ISourceObject
    {
        public int modelHeaderSize;
        public CompactSurfaceHeader compactSurfaceHeader;
        public LegacySurfaceHeader legacySurfaceHeader;
        public List<ConvexSolid> solids;
        public Vector3[] vertices;
        public string boneName;
        public CollisionData(int modelSize)
        {
            modelHeaderSize = modelSize;
        }
        public void ReadToObject(UReader reader, int version = 0)
        {
            long baseAddress = reader.BaseStream.Position;
            compactSurfaceHeader.ReadToObject(reader, version);
            legacySurfaceHeader.ReadToObject(reader, version);
            solids = new();

            // Read convex solids
            int iter = 0;
            while (true)  // FUCK THESE RIDICULOUS MEANS OF READING DATA
            {
                iter++;

                //Debug.Log($"Convex {iter - 1}, {reader.BaseStream.Position.ToString("X")}");

                long vertexBaseAddress = reader.BaseStream.Position;
                ConvexSolid convexSolid = reader.ReadSourceObject<ConvexSolid>();
                solids.Add(convexSolid);
                if (reader.BaseStream.Position == vertexBaseAddress + convexSolid.header.vertexOffset)
                    break;
                else if (reader.BaseStream.Position > vertexBaseAddress + convexSolid.header.vertexOffset)  // This shouldn't happen
                    return;
            }
            // Read vertices
            //long dataEndAddress = (baseAddress + modelHeaderSize);
            long dataEndAddress = (baseAddress + compactSurfaceHeader.size + 4);
            long dataLength = (dataEndAddress - reader.BaseStream.Position);
            int vertexCount = (int)(dataLength / (4 * 4));  // float4

            vertices = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = IConverter.TransformPointSourcePhysicsToUnity(reader.ReadVector4());
            }

            reader.BaseStream.Position = dataEndAddress;
        }
        public bool IsBoxShape(int partIndex, out Vector3 center, out Vector3 size)
        {
            //bool Compare(float a, float b)
            //{
            //    const float epsilon = 1f;
            //    return Mathf.Abs(a - b) <= epsilon;
            //}
            // TODO - Rotate so that normals are axis aligned

            // Normals must be orthogonal
            // Must have six planes / sides

            center = default;
            size = default;
            List<Plane> planes = new();

            TriangleData[] triangles = this.solids[partIndex].triangles;

            for (int t = 0; t < triangles.Length; t++)
            {
                Vector3 p1 = vertices[triangles[t].v1];
                Vector3 p2 = vertices[triangles[t].v2];
                Vector3 p3 = vertices[triangles[t].v3];

                Plane plane = new Plane(p3, p2, p1);

                //Debug.DrawRay(plane.normal * plane.distance, plane.normal * 0.1f, Color.cyan, 10.0f);

                const float angleTreshold = 2.0f;

                // Test if normal is orthogonal
                Vector3 normalAngles = Quaternion.LookRotation(plane.normal).eulerAngles;
                for (int c = 0; c < 3; c++)
                {
                    float angle = (normalAngles[c] % 90);
                    if (Mathf.Abs(angle) > angleTreshold && Mathf.Abs(angle - 90) > angleTreshold)
                    {
                        //Debug.Log($"Non orthogonal normal: {normalAngles[c] % 90}");
                        return false;
                    }
                }

                bool Equals(Plane x, Plane y)
                {
                    if (Mathf.Abs(Vector3.Angle(x.normal, y.normal)) > angleTreshold)
                        return false;

                    if (Mathf.Abs(x.distance - y.distance) > 0.0001f)
                        return false;

                    return true;
                }

                if (planes.Any<Plane>((Plane other) => { return Equals(plane, other); }) == false)
                    planes.Add(plane);
            }

            //Debug.Log(planes.Count);
            // The set of all unique planes has been made, there should be only six for the mesh to be considered a box
            if (planes.Count != 6)
                return false;

            // Get center/size
            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;

            for (int t = 0; t < triangles.Length; t++)
            {
                Vector3 p1 = vertices[triangles[t].v1];
                Vector3 p2 = vertices[triangles[t].v2];
                Vector3 p3 = vertices[triangles[t].v3];

                min = Vector3.Min(p1, min);
                max = Vector3.Max(p1, max);
                min = Vector3.Min(p2, min);
                max = Vector3.Max(p2, max);
                min = Vector3.Min(p3, min);
                max = Vector3.Max(p3, max);
            }

            center = (max + min) / 2.0f;
            size = max - min;

            return true;
        }
    }
    public class ConvexSolid : ISourceObject
    {
        public ConvexSolidHeader header;
        public TriangleData[] triangles;
         
        public void ReadToObject(UReader reader, int version = 0)
        {
            header = reader.ReadSourceObject<ConvexSolidHeader>(version);
            triangles = new TriangleData[header.triangleCount];
            reader.ReadSourceObjectArray(ref triangles, version);
        }
    }
}
