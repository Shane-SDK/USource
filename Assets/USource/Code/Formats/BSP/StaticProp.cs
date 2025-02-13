using UnityEngine;

namespace USource.Formats.BSP
{
    public struct StaticProp : ISourceObject
    {
        public Vector3 origin;
        public Vector3 angles;
        public ushort propType;
        public ushort firstLeaf;
        public ushort leafCount;
        public byte solid;
        public STATICPROP_FLAGS flags;
        public int skin;
        public float fadeMinDistance;
        public float fadeMaxDistance;
        public float forcedFadeScale;
        public Color32 color; // per instance color and alpha modulation
        public uint flagsExtra; // Further bitflags.
        public float scale; // Prop scale
        public void ReadToObject(UReader reader, int version = 0)  // version == prop version???
        {
            // v4 (IDFK)
            origin = Converters.IConverter.SourceTransformPoint(reader.ReadVector3());
            angles = Converters.IConverter.SourceTransformAngles(reader.ReadVector3());
            propType = reader.ReadUInt16();
            firstLeaf = reader.ReadUInt16();
            leafCount = reader.ReadUInt16();
            solid = reader.ReadByte();
            flags = (STATICPROP_FLAGS)reader.ReadByte();
            skin = reader.ReadInt32();
            fadeMinDistance = reader.ReadSingle();
            fadeMaxDistance = reader.ReadSingle();
            reader.Skip(4 * 3);  // Vector3 lighting origin

            if (version >= 5)
            {
                forcedFadeScale = reader.ReadSingle();
            }

            if (version >= 6)
            {
                reader.Skip(4);  // either two ushorts or 4 bytes for hardware settings
            }

            if (version >= 7)
            {
                color = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            }

            if (version >= 9)
            {
                reader.Skip(4);  // int32 xbox flag
            }

            if (version >= 10)
            {
                flagsExtra = reader.ReadUInt32();
            }

            if (version >= 11)
            {
                scale = reader.ReadSingle();
            }
        }
    }
}