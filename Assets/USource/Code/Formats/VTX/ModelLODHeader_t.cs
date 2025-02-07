namespace USource.Formats.VTX
{
    public struct ModelLODHeader_t : ISourceObject
    {
        public int numMeshes;
        public int meshOffset;
        public float switchPoint;

        public void ReadToObject(UReader reader, int version = 0)
        {
            numMeshes = reader.ReadInt32();
            meshOffset = reader.ReadInt32();
            switchPoint = reader.ReadSingle();
        }
    }
}