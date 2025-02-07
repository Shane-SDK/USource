namespace USource.Formats.BSP
{
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
}
