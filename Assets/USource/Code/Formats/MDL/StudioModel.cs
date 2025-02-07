namespace USource.Formats.MDL
{
    public struct StudioModel : ISourceObject
    {
        public string name;

        public int type;
        public float boundingradius;
        public int nummeshes;
        public int meshindex;

        public int numvertices;
        public int vertexindex;
        public int tangentsindex;

        public int numattachments;
        public int attachmentindex;

        public int numeyeballs;
        public int eyeballindex;

        public StudioModelVertexData vertexdata;
        public void ReadToObject(UReader reader, int version = 0)
        {
            name = Converters.IConverter.ByteArrayToString(reader.ReadBytes(64));
            type = reader.ReadInt32();
            boundingradius = reader.ReadSingle();
            nummeshes = reader.ReadInt32();
            meshindex = reader.ReadInt32();

            numvertices = reader.ReadInt32();
            vertexindex = reader.ReadInt32();
            tangentsindex = reader.ReadInt32();

            numattachments = reader.ReadInt32();
            attachmentindex = reader.ReadInt32();

            numeyeballs = reader.ReadInt32();
            eyeballindex = reader.ReadInt32();

            vertexdata = reader.ReadSourceObject<StudioModelVertexData>(version);

            reader.Skip(4 * 8);  // unused int32 array of size 8
        }
    }
}
