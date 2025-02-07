using UnityEngine;

namespace USource.Formats.BSP
{
    public struct DisplacementVertex : ISourceObject
    {
        public Vector3 displacement;    // Vector field defining displacement volume.
        public float distance;  // Displacement distances.
        public float alpha; // "per vertex" alpha values.

        public void ReadToObject(UReader reader, int version = 0)
        {
            displacement = reader.ReadVector3();
            distance = reader.ReadSingle();
            alpha = reader.ReadSingle();
        }
    }
}
