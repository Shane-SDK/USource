using UnityEngine;

namespace USource.Formats.MDL
{
    public struct StudioVertex : ISourceObject
    {
        public StudioBoneWeight m_BoneWeights;
        public Vector3 m_vecPosition;
        public Vector3 m_vecNormal;
        public Vector2 m_vecTexCoord;

        public void ReadToObject(UReader reader, int version = 0)
        {
            m_BoneWeights = reader.ReadSourceObject<StudioBoneWeight>();
            m_vecPosition = Converters.IConverter.SourceTransformPoint(reader.ReadVector3());
            m_vecNormal = Converters.IConverter.SourceTransformDirection(reader.ReadVector3());
            m_vecTexCoord = reader.ReadVector2();
            m_vecTexCoord.y *= -1;
            m_vecTexCoord.y += 1;
        }
    }
}
