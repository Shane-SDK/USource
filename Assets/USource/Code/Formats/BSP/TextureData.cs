using UnityEngine;

namespace USource.Formats.BSP
{
    public struct TextureData : ISourceObject
    {
        public Vector3 reflectivity;    // RGB reflectivity
        public int nameStringTableIndex;    // index into TexdataStringTable
        public int width, height;   // source image
        public int viewWidth, viewHeight;

        public void ReadToObject(UReader reader, int version = 0)
        {
            reflectivity.x = reader.ReadSingle();
            reflectivity.y = reader.ReadSingle();
            reflectivity.z = reader.ReadSingle();

            nameStringTableIndex = reader.ReadInt32();

            width = reader.ReadInt32();
            height = reader.ReadInt32();

            viewWidth = reader.ReadInt32();
            viewHeight = reader.ReadInt32();
        }
    }
}
