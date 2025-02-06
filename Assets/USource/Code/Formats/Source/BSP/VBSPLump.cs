using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace USource.Formats.Source.BSP
{
    [Flags]
    public enum STATICPROP_FLAGS
    {
        /// <summary>automatically computed</summary>
        STATIC_PROP_FLAG_FADES = 0x1,
        /// <summary>automatically computed</summary>
        STATIC_PROP_USE_LIGHTING_ORIGIN = 0x2,
        /// <summary>automatically computed; computed at run time based on dx level</summary>
        STATIC_PROP_NO_DRAW = 0x4,

        /// <summary>set in WC</summary>
        STATIC_PROP_IGNORE_NORMALS = 0x8,
        /// <summary>set in WC</summary>
        STATIC_PROP_NO_SHADOW = 0x10,
        /// <summary>set in WC</summary>
        STATIC_PROP_UNUSED = 0x20,

        /// <summary>in vrad, compute lighting at lighting origin, not for each vertex</summary>
        STATIC_PROP_NO_PER_VERTEX_LIGHTING = 0x40,

        /// <summary>disable self shadowing in vrad</summary>
        STATIC_PROP_NO_SELF_SHADOWING = 0x80
    }
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