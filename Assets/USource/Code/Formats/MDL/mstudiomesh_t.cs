using UnityEngine;

namespace USource.Formats.MDL
{
    public struct mstudiomesh_t : ISourceObject
    {
        public int material;
        public int modelindex;
        public int numvertices;
        public int vertexoffset;
        public int numflexes;
        public int flexindex;
        public int materialtype;
        public int materialparam;
        public int meshid;
        public Vector3 center;
        public StudioMeshVertexData VertexData;

        public void ReadToObject(UReader reader, int version = 0)
        {
            material = reader.ReadInt32();
            modelindex = reader.ReadInt32();
            numvertices = reader.ReadInt32();
            vertexoffset = reader.ReadInt32();
            numflexes = reader.ReadInt32();
            flexindex = reader.ReadInt32();
            materialtype = reader.ReadInt32();
            materialparam = reader.ReadInt32();
            meshid = reader.ReadInt32();
            center = reader.ReadVector3();
            VertexData = reader.ReadSourceObject<StudioMeshVertexData>(version);
            reader.Skip(4 * 8);  // unused int32 array of size 8
        }
    }
}
