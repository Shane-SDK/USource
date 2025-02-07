using UnityEngine;

namespace USource.Formats.BSP
{
    public struct TextureInfo : ISourceObject
    {
        public Vector4 textureVecs0;   // [s/t][xyz offset]
        public Vector4 textureVecs1;   // [s/t][xyz offset]
        public Vector4 lightmapVecs0;  // [s/t][xyz offset] - length is in units of texels/area
        public Vector4 lightmapVecs1;  // [s/t][xyz offset] - length is in units of texels/area
        public SurfFlags flags; // miptex flags overrides
        public int textureDataIndex; // Pointer to texture name, size, etc.

        public void ReadToObject(UReader reader, int version = 0)
        {
            textureVecs0 = reader.ReadVector4();
            textureVecs1 = reader.ReadVector4();
            lightmapVecs0 = reader.ReadVector4();
            lightmapVecs1 = reader.ReadVector4();

            flags = (SurfFlags)reader.ReadInt32();
            textureDataIndex = reader.ReadInt32();
        }
    }
}
