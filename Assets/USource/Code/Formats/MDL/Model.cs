using System.Collections.Generic;

namespace USource.Formats.MDL
{
    public struct Model
    {
        public bool isBlank;
        public StudioModel model;
        public mstudiomesh_t[] Meshes;
        public Dictionary<int, List<int>>[] IndicesPerLod;
        public StudioVertex[][] VerticesPerLod;
    }
}
