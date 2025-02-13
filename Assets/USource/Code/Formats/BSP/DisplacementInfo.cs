using UnityEngine;

namespace USource.Formats.BSP
{
    public struct DisplacementInfo : ISourceObject
    {
        public Vector3 startPosition;   // start position used for orientation
        public int displacementVertexStart;   // Index into LUMP_DISP_VERTS.
        public int displacementTriangleStart;    // Index into LUMP_DISP_TRIS.
        public int power;   // power - indicates size of surface (2^power 1)
        public int minimumTesselation; // minimum tesselation allowed
        public float smoothingAngle;    // lighting smoothing angle
        public int content;    // surface contents
        public ushort face;  // Which map face this displacement comes from.
        public int lightMapAlpha;  // Index into ddisplightmapalpha.
        public int lightMapStart; // Index into LUMP_DISP_LIGHTMAP_SAMPLE_POSITIONS.
        public void ReadToObject(UReader reader, int version = 0)
        {
            startPosition = reader.ReadVector3();
            displacementVertexStart = reader.ReadInt32();
            displacementTriangleStart = reader.ReadInt32();
            power = reader.ReadInt32();
            minimumTesselation = reader.ReadInt32();
            smoothingAngle = reader.ReadSingle();
            content = reader.ReadInt32();
            face = reader.ReadUInt16();
            lightMapAlpha = reader.ReadInt32();
            lightMapStart = reader.ReadInt32();

            // skip 130 bytes, apparently unnecessary
            reader.Skip(130);
        }
    }
}
