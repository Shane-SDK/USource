using UnityEngine;

namespace USource.Formats.MDL
{
    public struct StudioBoneWeight : ISourceObject
    {
        public Vector3 weight;
        public byte[] bone;
        public byte numbones;

        public void ReadToObject(UReader reader, int version = 0)
        {
            weight = reader.ReadVector3();
            bone = reader.ReadBytes(3);
            numbones = reader.ReadByte();
        }
    }
}
