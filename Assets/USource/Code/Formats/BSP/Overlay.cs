using UnityEngine;

namespace USource.Formats.BSP
{
    public struct Overlay : ISourceObject
    {
        public int id;  //Special ID  
        public short textureInfo;   //Texture Info
        public ushort faceCountRenderOrder;
        public int[] faces;
        public Vector2 u;
        public Vector2 v;
        public Vector3[] uvPoints;
        public Vector3 origin;
        public Vector3 normal;

        public void ReadToObject(UReader reader, int version = 0)
        {
            id = reader.ReadInt32();
            textureInfo = reader.ReadInt16();
            faceCountRenderOrder = reader.ReadUInt16();
            faces = reader.ReadIntArray(64);
            u = reader.ReadVector2();
            v = reader.ReadVector2();
            uvPoints = new Vector3[4];
            reader.ReadVector3Array(uvPoints);
            origin = reader.ReadVector3();
            normal = reader.ReadVector3();
        }
    }
}
