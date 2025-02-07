namespace USource.Formats.BSP
{
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
}
