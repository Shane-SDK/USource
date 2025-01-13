using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
    }

    public struct PhysPart
    {
        public int[] triangles;
    }
}
