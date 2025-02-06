namespace USource.Formats.Source.PHYS
{
    public class ConvexSolid : ISourceObject
    {
        public ConvexSolidHeader header;
        public TriangleData[] triangles;
         
        public void ReadToObject(UReader reader, int version = 0)
        {
            header = reader.ReadSourceObject<ConvexSolidHeader>(version);
            triangles = new TriangleData[header.triangleCount];
            reader.ReadSourceObjectArray(ref triangles, version);
        }
    }
}
