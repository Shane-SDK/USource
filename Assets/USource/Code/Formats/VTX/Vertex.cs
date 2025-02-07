namespace USource.Formats.VTX
{
    public struct Vertex : ISourceObject
    {
        public byte[] boneWeightIndices;
        public byte numBones;
        public ushort origMeshVertId;
        public byte[] boneID;

        public void ReadToObject(UReader reader, int version = 0)
        {
            boneWeightIndices = reader.ReadBytes(3);
            numBones = reader.ReadByte();
            origMeshVertId = reader.ReadUInt16();

            boneID = reader.ReadBytes(3);
        }
    }
}