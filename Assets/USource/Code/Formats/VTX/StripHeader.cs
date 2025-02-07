namespace USource.Formats.VTX
{
    public struct StripHeader : ISourceObject
    {
        // indexOffset offsets into the mesh's index array.
        public int numIndices;
        public int indexOffset;

        // vertexOffset offsets into the mesh's vert array.
        public int numVerts;
        public int vertOffset;

        // use this to enable/disable skinning.  
        // May decide (in optimize.cpp) to put all with 1 bone in a different strip 
        // than those that need skinning.
        public short numBones;

        public byte flags;

        public int numBoneStateChanges;
        public int boneStateChangeOffset;

        public void ReadToObject(UReader reader, int version = 0)
        {
            numIndices = reader.ReadInt32();
            indexOffset = reader.ReadInt32();
            numVerts = reader.ReadInt32();
            vertOffset = reader.ReadInt32();
            numBones = reader.ReadInt16();
            flags = reader.ReadByte();
            numBoneStateChanges = reader.ReadInt32();
            boneStateChangeOffset = reader.ReadInt32();
        }

        //TODO: Some custom engines / games has this bytes, like a Alien Swarm / CSGO / DOTA2 (except L4D and L4D2?)
        // These go last on purpose!
        //public Int32 numTopologyIndices;
        //public Int32 topologyOffset;
    }
}