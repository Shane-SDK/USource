using System;
using System.Runtime.InteropServices;

namespace USource.Formats.BSP
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
}