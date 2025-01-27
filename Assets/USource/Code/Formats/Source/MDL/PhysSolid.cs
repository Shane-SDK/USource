using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static USource.Formats.Source.MDL.MDLFile;

namespace USource.Formats.Source.MDL
{
    public struct PhysSolid
    {
        const float AngleThreshold = 2.0f;
        public UnityEngine.Vector3[] vertices;
        public PhysPart[] parts;
        public string boneName;
        public float mass;
        public int index;

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

            int[] triangles = this.parts[partIndex].triangles;

            for (int t = 0; t < triangles.Length / 3; t++)
            {
                Vector3 p1 = vertices[triangles[t * 3]];
                Vector3 p2 = vertices[triangles[t * 3 + 1]];
                Vector3 p3 = vertices[triangles[t * 3 + 2]];

                Plane plane = new Plane(p3, p2, p1);

                //Debug.DrawRay(plane.normal * plane.distance, plane.normal * 0.1f, Color.cyan, 10.0f);

                // Test if normal is orthogonal
                Vector3 normalAngles = Quaternion.LookRotation(plane.normal).eulerAngles;
                for (int c = 0; c < 3; c++)
                {
                    float angle = (normalAngles[c] % 90);
                    if (Mathf.Abs(angle) > AngleThreshold && Mathf.Abs(angle - 90) > AngleThreshold)
                    {
                        //Debug.Log($"Non orthogonal normal: {normalAngles[c] % 90}");
                        return false;
                    }
                }

                bool Equals(Plane x, Plane y)
                {
                    if (Mathf.Abs(Vector3.Angle(x.normal, y.normal)) > AngleThreshold)
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
            Bounds bounds = new Bounds(vertices[triangles[0]], Vector3.zero);

            for (int i = 1; i < triangles.Length; i++)
                bounds.Encapsulate(vertices[triangles[i]]);

            center = bounds.center;
            size = bounds.size;

            return true;
        }
        public static PhysSolid[] ReadCollisionData(UReader reader, int solidCount, bool isStatic)
        {
            const float physicsScalingFactor = 1.016f;
            PhysSolid[] physSolids = new PhysSolid[solidCount];
            int partCount = 0;

            for (int i = 0; i < solidCount; i++)
            {
                // Each solid can be made up of separate bodies of vertices
                List<List<int>> indexSet = new List<List<int>>();
                compactsurfaceheader_t compactHeader = default;
                long nextHeader = reader.BaseStream.Position;
                reader.ReadType<compactsurfaceheader_t>(ref compactHeader);
                nextHeader += compactHeader.size + sizeof(int);

                legacysurfaceheader_t legacyHeader = default;
                reader.ReadType<legacysurfaceheader_t>(ref legacyHeader);

                long verticesPosition = 0;
                int largestVertexIndex = -1;

                // The number of separate bodies in a solid seems to be unknown so stop once the beginning of the vertex offset is reached
                while ((reader.BaseStream.Position < verticesPosition || largestVertexIndex == -1) && reader.BaseStream.Position < reader.BaseStream.Length)  // Read triangles until the vertex offset is reached
                {
                    partCount++;
                    List<int> indices = new List<int>();
                    indexSet.Add(indices);
                    trianglefaceheader_t triangleFaceHeader = default;
                    long headerPosition = reader.BaseStream.Position;
                    reader.ReadType<trianglefaceheader_t>(ref triangleFaceHeader);

                    verticesPosition = headerPosition + triangleFaceHeader.m_offsetTovertices;

                    //BitArray bitSet = new BitArray(triangleFaceHeader.dummy[1]);
                    //bool skipData = bitSet[0];

                    //if (skipData)
                    //    break;

                    triangleface_t[] triangleFaces = new triangleface_t[triangleFaceHeader.m_countFaces];
                    reader.ReadArray<triangleface_t>(ref triangleFaces);

                    for (int t = 0; t < triangleFaces.Length; t++)
                    {
                        triangleface_t face = triangleFaces[t];
                        indices.Add(face.v3);
                        indices.Add(face.v2);
                        indices.Add(face.v1);

                        // Get the largest vertex index
                        int max = Mathf.Max(face.v1, face.v2, face.v3);
                        if (max > largestVertexIndex)
                            largestVertexIndex = max;
                    }
                }

                vertex[] vertices = new vertex[largestVertexIndex + 1];
                reader.ReadArray<vertex>(ref vertices, verticesPosition);

                PhysSolid solid = default;
                solid.index = -1;
                solid.parts = new PhysPart[indexSet.Count];
                for (int p = 0; p < indexSet.Count; p++)
                    solid.parts[p].triangles = indexSet[p].ToArray();

                solid.vertices = new Vector3[vertices.Length];

                for (int t = 0; t < vertices.Length; t++)
                {
                    solid.vertices[t] = Quaternion.AngleAxis(180, Vector3.up) * Quaternion.AngleAxis(isStatic ? 90 : 0, Vector3.right) * new Vector3(
                        vertices[t].position[0],
                        vertices[t].position[2],
                        vertices[t].position[1]) / physicsScalingFactor;
                }

                physSolids[i] = solid;
                reader.BaseStream.Position = nextHeader;
            }

            //// Read text at the end of file
            //byte[] bytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            //string text = System.Text.Encoding.ASCII.GetString(bytes);
            //KeyValues keyValues = KeyValues.Parse(text);
            //foreach (KeyValues.Entry entry in keyValues["solid"])
            //{
            //    if (int.TryParse(entry["index"], out int index))
            //    {
            //        PhysSolid solid = physSolids[index];
            //        solid.index = index;
            //        if (float.TryParse(entry["mass"], out float mass))
            //        {
            //            solid.mass = mass;
            //        }
            //        solid.boneName = entry["name"];

            //        physSolids[index] = solid;
            //    }
            //}

            return physSolids;
        }
    }
    public struct PhysPart
    {
        public int[] triangles;
    }
}
