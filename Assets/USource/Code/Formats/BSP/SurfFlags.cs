namespace USource.Formats.BSP
{
    public enum SurfFlags
    {
        SURF_LIGHT = 0x0001,        // value will hold the light strength
        SURF_SLICK = 0x0002,        // effects game physics
        SURF_SKY = 0x0004,          // don't draw, but add to skybox
        SURF_WARP = 0x0008,         // turbulent water warp
        SURF_TRANS = 0x0010,        // surface is transparent
        SURF_WET = 0x0020,          // the surface is wet
        SURF_FLOWING = 0x0040,      // scroll towards angle
        SURF_NODRAW = 0x0080,       // don't bother referencing the texture
        SURF_HINT = 0x0100,         // make a primary bsp splitter
        SURF_SKIP = 0x0200,         // completely ignore, allowing non-closed brushes
        SURF_NOLIGHT = 0x0400,      // Don't calculate light on this surface
        SURF_BUMPLIGHT = 0x0800,    // calculate three lightmaps for the surface for bumpmapping
        SURF_NOSHADOWS = 0x1000,    // Don't receive shadows
        SURF_NODECALS = 0x2000,     // Don't receive decals
        SURF_NOCHOP = 0x4000,       // Don't subdivide patches on this surface
        SURF_HITBOX = 0x8000        // surface is part of a hitbox
    }
}
