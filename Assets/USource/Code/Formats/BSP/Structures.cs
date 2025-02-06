using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Unity.Mathematics;
using USource.Converters;
using System.Collections.Generic;

namespace USource.Formats.BSP
{
    public struct Header : ISourceObject
    {
        public int identity;  // BSP file identifier
        public int version;  // BSP file version
        public LumpHeader[] lumps;
        public int mapRevision;  // the map's revision (iteration, version) number

        public void ReadToObject(UReader reader, int version = 0)
        {
            identity = reader.ReadInt32();
            this.version = reader.ReadInt32();

            lumps = new LumpHeader[64];
            reader.ReadSourceObjectArray(ref lumps, version);

            mapRevision = reader.ReadInt32();
        }
    }
    public struct LumpHeader : ISourceObject
    {
        public int fileOffset;
        // offset into file (bytes)
        public int fileLength;
        // length of lump (bytes)
        public int version;
        // lump format version
        public int code;
        // lump ident code

        public void ReadToObject(UReader reader, int version = 0)
        {
            fileOffset = reader.ReadInt32();
            fileLength = reader.ReadInt32();
            this.version = reader.ReadInt32();
            code = reader.ReadInt32();


        }
    }
    public struct Edge : ISourceObject
    {
        public ushort index0;
        public ushort index1;

        public void ReadToObject(UReader reader, int version = 0)
        {
            index0 = reader.ReadUInt16();
            index1 = reader.ReadUInt16();
        }
    }
    public struct Face : ISourceObject
    {
        public ushort planeIndex;   // the plane number
        public byte side;   // faces opposite to the node's plane direction
        public byte onNode; // 1 of on node, 0 if in leaf
        public int firstEdgeIndex;  // index into surfedges
        public short edgeCount; // number of surfedges
        public short textureInfo;   // texture info
        public short displacementInfo;  // displacement info
        public short surfaceFogVolumeId;    // ?
        public byte style0; // switchable lighting info
        public byte style1;
        public byte style2;
        public byte style3;
        public int lightOffset; // offset into lightmap lump
        public float area;  // face area in units^2
        public int lightmapTextureLuxelMin0;    // texture lighting info
        public int lightmapTextureLuxelMin1;
        public int lightmapTextureLuxelSize0;
        public int lightmapTextureLuxelSize1;   // texture lighting info
        public int originalFace;    // original face this was split from
        public ushort primitiveCount;   // primitives
        public ushort firstPrimitiveIndex;
        public uint smoothingGroup; // lightmap smoothing group

        public void ReadToObject(UReader reader, int version = 0)
        {
            planeIndex = reader.ReadUInt16();
            side = reader.ReadByte();
            onNode = reader.ReadByte();
            firstEdgeIndex = reader.ReadInt32();
            edgeCount = reader.ReadInt16();
            textureInfo = reader.ReadInt16();
            displacementInfo = reader.ReadInt16();
            surfaceFogVolumeId = reader.ReadInt16();
            style0 = reader.ReadByte();
            style1 = reader.ReadByte();
            style2 = reader.ReadByte();
            style3 = reader.ReadByte();
            lightOffset = reader.ReadInt32();
            area = reader.ReadSingle();
            lightmapTextureLuxelMin0 = reader.ReadInt32();
            lightmapTextureLuxelMin1 = reader.ReadInt32();
            lightmapTextureLuxelSize0 = reader.ReadInt32();
            lightmapTextureLuxelSize1 = reader.ReadInt32();
            originalFace = reader.ReadInt32();
            primitiveCount = reader.ReadUInt16();
            firstPrimitiveIndex = reader.ReadUInt16();
            smoothingGroup = reader.ReadUInt32();
        }
    }
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
    public struct BrushModel : ISourceObject
    {
        public Vector3 min, max;  // bounding box
        public Vector3 origin;  // for sounds or lights
        public int headNode;    // index into node array
        public int firstFace, faceCount; // index into face array

        public void ReadToObject(UReader reader, int version = 0)
        {
            min = reader.ReadVector3();
            max = reader.ReadVector3();

            origin = reader.ReadVector3();

            headNode = reader.ReadInt32();
            firstFace = reader.ReadInt32();
            faceCount = reader.ReadInt32();
        }
    }
    public struct PhysicsBrushModel : ISourceObject
    {
        public int modelIndex;  // Perhaps the index of the model to which this physics model applies?
        public int dataSize;    // Total size of the collision data sections
        public int keyDataSize; // Size of the text section
        public int solidCount;  // Number of collision data sections

        public void ReadToObject(UReader reader, int version = 0)
        {
            modelIndex = reader.ReadInt32();
            dataSize = reader.ReadInt32();
            keyDataSize = reader.ReadInt32();
            solidCount = reader.ReadInt32();
        }
    }
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
    public struct GameLumpHeader : ISourceObject
    {
        public int id;  // gamelump ID
        public ushort flags;    // flags
        public ushort version;  // gamelump version
        public int fileOffset;  // offset to this gamelump
        public int fileLength;  // length

        public void ReadToObject(UReader reader, int version = 0)
        {
            id = reader.ReadInt32();
            flags = reader.ReadUInt16();
            this.version = reader.ReadUInt16();
            fileOffset = reader.ReadInt32();
            fileLength = reader.ReadInt32();
        }
    }
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
            reader.BaseStream.Seek(130, System.IO.SeekOrigin.Begin);
        }
    }
    public struct DisplacementVertex : ISourceObject
    {
        public Vector3 displacement;    // Vector field defining displacement volume.
        public float distance;  // Displacement distances.
        public float alpha; // "per vertex" alpha values.

        public void ReadToObject(UReader reader, int version = 0)
        {
            displacement = reader.ReadVector3();
            distance = reader.ReadSingle();
            alpha = reader.ReadSingle();
        }
    }
    public struct Node : ISourceObject
    {
        public int plane;
        public int2 children;
        public short minX;
        public short minY;
        public short minZ;
        public short maxX;
        public short maxY;
        public short maxZ;
        public ushort firstFace;
        public ushort faceCount;
        public short area;
        public short padding;

        public Vector3 TransformMin()
        {
            Vector3 min = IConverter.SourceTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = IConverter.SourceTransformPoint(new Vector3(maxX, maxY, maxZ));
            return new Vector3(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Min(min.z, max.z));
        }
        public Vector3 TransformMax()
        {
            Vector3 min = IConverter.SourceTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = IConverter.SourceTransformPoint(new Vector3(maxX, maxY, maxZ));
            return new Vector3(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y), Mathf.Max(min.z, max.z));
        }
        public bool Contains(Vector3 c, float tolerance = 0.01f)
        {
            Vector3 min = TransformMin();
            Vector3 max = TransformMax();
            Bounds bounds = new Bounds { min = min - Vector3.one * tolerance, max = max + Vector3.one * tolerance };
            return bounds.Contains(c);
        }
        public void Draw(Color color)
        {
            Bounds bounds = new Bounds { min = TransformMin(), max = TransformMax() };
            Conversions.DrawBox(bounds.center, Quaternion.identity, bounds.size, color, 10.0f);
        }
        public void ReadToObject(UReader reader, int version = 0)
        {
            plane = reader.ReadInt32();
            children.x = reader.ReadInt32();
            children.y = reader.ReadInt32();
            minX = reader.ReadInt16();
            minY = reader.ReadInt16();
            minZ = reader.ReadInt16();
            maxX = reader.ReadInt16();
            maxY = reader.ReadInt16();
            maxZ = reader.ReadInt16();
            firstFace = reader.ReadUInt16();
            faceCount = reader.ReadUInt16();
            area = reader.ReadInt16();
            padding = reader.ReadInt16();
        }
    }
    public struct Leaf : ISourceObject
    {
        public int contents;
        public short cluster;
        public short areaFlags;
        public short minX;
        public short minY;
        public short minZ;
        public short maxX;
        public short maxY;
        public short maxZ;
        public ushort firstLeafFace;
        public ushort leafFaceCount;
        public ushort firstLeafBrush;
        public ushort leafBrushCount;
        public short leafWaterDataID;
        public short padding;

        public Vector3 TransformMin()
        {
            Vector3 min = IConverter.SourceTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = IConverter.SourceTransformPoint(new Vector3(maxX, maxY, maxZ));
            return new Vector3(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Min(min.z, max.z));
        }
        public Vector3 TransformMax()
        {
            Vector3 min = IConverter.SourceTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = IConverter.SourceTransformPoint(new Vector3(maxX, maxY, maxZ));
            return new Vector3(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y), Mathf.Max(min.z, max.z));
        }
        public bool Contains(Vector3 c)
        {
            Vector3 min = TransformMin();
            Vector3 max = TransformMax();
            Bounds bounds = new Bounds { min = min - Vector3.one * 0.01f, max = max + Vector3.one * 0.01f };
            return bounds.Contains(c);
        }
        public void Draw(Color color)
        {
            Bounds bounds = new Bounds { min = TransformMin(), max = TransformMax() };
            Conversions.DrawBox(bounds.center, Quaternion.identity, bounds.size, color, 10.0f);
        }

        public void ReadToObject(UReader reader, int version = 0)
        {
            contents = reader.ReadInt32();
            cluster = reader.ReadInt16();
            areaFlags = reader.ReadInt16();
            minX = reader.ReadInt16();
            minY = reader.ReadInt16();
            minZ = reader.ReadInt16();
            maxX = reader.ReadInt16();
            maxY = reader.ReadInt16();
            maxZ = reader.ReadInt16();
            firstLeafFace = reader.ReadUInt16();
            leafFaceCount = reader.ReadUInt16();
            firstLeafBrush = reader.ReadUInt16();
            leafBrushCount = reader.ReadUInt16();
            leafWaterDataID = reader.ReadInt16();
            padding = reader.ReadInt16();
        }
    }
    public struct Plane : ISourceObject
    {
        public Vector3 normal;
        public float distance;
        public int type;

        public void ReadToObject(UReader reader, int version = 0)
        {
            normal = reader.ReadVector3();
            distance = reader.ReadSingle();
            type = reader.ReadInt32();
        }
    }
    public struct LeafAmbientLighting : ISourceObject
    {
        public CompressedLightCube cube;
        public byte x;
        public byte y;
        public byte z;
        public byte pad;
        public void ReadToObject(UReader reader, int version = 0)
        {
            cube.ReadToObject(reader, version);
            x = reader.ReadByte();
            y = reader.ReadByte();
            z = reader.ReadByte();
            pad = reader.ReadByte();
        }
    }
    public struct CompressedLightCube : ISourceObject
    {
        public ColorRGBExp32 color0, color1, color2, color3, color4, color5;

        public void ReadToObject(UReader reader, int version = 0)
        {
            color0.ReadToObject(reader, version);
            color1.ReadToObject(reader, version);
            color2.ReadToObject(reader, version);
            color3.ReadToObject(reader, version);
            color4.ReadToObject(reader, version);
            color5.ReadToObject(reader, version);
        }
    }
    public struct ColorRGBExp32 : ISourceObject
    {
        public byte r;
        public byte g;
        public byte b;
        public sbyte exponent;
        public void ReadToObject(UReader reader, int version = 0)
        {
            r = reader.ReadByte();
            g = reader.ReadByte();
            b = reader.ReadByte();
            exponent = (sbyte)reader.ReadByte();
        }
    }
    public struct LeafAmbientIndex : ISourceObject
    {
        public ushort ambientSampleCount;
        public ushort firstAmbientSample;

        public void ReadToObject(UReader reader, int version = 0)
        {
            ambientSampleCount = reader.ReadUInt16();
            firstAmbientSample = reader.ReadUInt16();
        }
    }
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
