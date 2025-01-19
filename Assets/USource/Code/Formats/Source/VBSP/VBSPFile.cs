using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace USource.Formats.Source.VBSP
{
    // https://developer.valvesoftware.com/wiki/Source_BSP_File_Format#Lump_structure
    public class VBSPFile : VBSPStruct
    {
        // ======== BSP ======= //
        public UReader reader;
        public dheader_t header;

        public dface_t[] faces;
        public dmodel_t[] models;

        public doverlay_t[] overlays;

        public ddispinfo_t[] dispInfo;
        public dDispVert[] dispVerts;

        public texinfo_t[] texInfo;
        public dtexdata_t[] texData;
        public String[] textureStringData;

        public dedge_t[] edges;
        public Vector3[] vertices;
        public Int32[] surfEdges;

        public dnode_t[] nodes;
        public dleaf_t[] leafs;
        public ushort[] leafFaces;

        // ======== OTHER ======= //

        public Face[] cFaces, cDisps;
        public string name;

        public List<BspEntity> entities;

        public PAKProvider pakProvider = null;
        //TODO: Check if LUMPs has a LZMA compression (ex: updated tf maps)
        public VBSPFile(Stream stream, string BSPName = default)
        {
            //bspPath = uLoader.RootPath + "/" + uLoader.ModFolders[0] + "/maps/" + BSPName + ".bsp";
            //if (!File.Exists(bspPath))
            //    throw new FileNotFoundException(String.Format("Map file ({0}) wasn't found in the ({1}) mod-folder. Check weather a path is valid.", BSPName, uLoader.ModFolders[0]));
            name = BSPName;
            //bspPath = BSPName;
            reader = new UReader(stream);
            reader.ReadType(ref header);

            if (header.Ident != 0x50534256)
                throw new FileLoadException(String.Format("{0}: File signature does not match 'VBSP'", BSPName));

            if (header.Version < 19 || header.Version > 21)
                throw new FileLoadException(String.Format("{0}: BSP version ({1}) isn't supported", BSPName, header.Version));

            if (header.Lumps[0].FileOfs == 0)
            {
                Debug.Log("Found Left 4 Dead 2 header");
                for (Int32 i = 0; i < header.Lumps.Length; i++)
                {
                    header.Lumps[i].FileOfs = header.Lumps[i].FileLen;
                    header.Lumps[i].FileLen = header.Lumps[i].Version;
                }
            }

            //BSP_WorldSpawn = new GameObject(BSPName);

            if (header.Lumps[58].FileLen / 56 <= 0)
            {
                faces = new dface_t[header.Lumps[7].FileLen / 56];
                reader.ReadArray(ref faces, header.Lumps[7].FileOfs);
            }
            else
            {
                faces = new dface_t[header.Lumps[58].FileLen / 56];
                reader.ReadArray(ref faces, header.Lumps[58].FileOfs);
            }

            models = new dmodel_t[header.Lumps[14].FileLen / 48];
            reader.ReadArray(ref models, header.Lumps[14].FileOfs);

            overlays = new doverlay_t[header.Lumps[45].FileLen / 352];
            reader.ReadArray(ref overlays, header.Lumps[45].FileOfs);

            dispInfo = new ddispinfo_t[header.Lumps[26].FileLen / 176];
            reader.ReadArray(ref dispInfo, header.Lumps[26].FileOfs);

            dispVerts = new dDispVert[header.Lumps[33].FileLen / 20];
            reader.ReadArray(ref dispVerts, header.Lumps[33].FileOfs);

            texInfo = new texinfo_t[header.Lumps[18].FileLen / 12];
            reader.ReadArray(ref texInfo, header.Lumps[18].FileOfs);

            texInfo = new texinfo_t[header.Lumps[6].FileLen / 72];
            reader.ReadArray(ref texInfo, header.Lumps[6].FileOfs);

            texData = new dtexdata_t[header.Lumps[2].FileLen / 32];
            reader.ReadArray(ref texData, header.Lumps[2].FileOfs);

            textureStringData = new String[header.Lumps[44].FileLen / 4];

            Int32[] BSP_TextureStringTable = new Int32[header.Lumps[44].FileLen / 4];
            reader.ReadArray(ref BSP_TextureStringTable, header.Lumps[44].FileOfs);

            //if (ULoader.ModFolders[0] == "tf")
            //    return file;

            for (Int32 i = 0; i < BSP_TextureStringTable.Length; i++)
                textureStringData[i] = reader.ReadNullTerminatedString(header.Lumps[43].FileOfs + BSP_TextureStringTable[i]);

            edges = new dedge_t[header.Lumps[12].FileLen / 4];
            reader.ReadArray(ref edges, header.Lumps[12].FileOfs);

            reader.BaseStream.Seek(header.Lumps[3].FileOfs, SeekOrigin.Begin);
            vertices = new Vector3[header.Lumps[3].FileLen / 12];

            for (Int32 i = 0; i < vertices.Length; i++)
                vertices[i] = reader.ReadVector3D(true) * USource.settings.sourceToUnityScale;

            surfEdges = new Int32[header.Lumps[13].FileLen / 4];
            reader.ReadArray(ref surfEdges, header.Lumps[13].FileOfs);

            entities = new();
            reader.BaseStream.Seek(header.Lumps[0].FileOfs, SeekOrigin.Begin);
            MatchCollection Matches = Regex.Matches(
                new(reader.ReadChars(header.Lumps[0].FileLen)),
                @"{[^}]*}", RegexOptions.IgnoreCase);

            int[] quoteIndexBuffer = new int[4];
            foreach(Match m in Matches)
            {
                string[] lines = m.Value.Trim('{', '}', ' ').Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) continue;
                BspEntity ent = new BspEntity();
                foreach (string line in lines)
                {
                    int quoteCount = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '"')
                        {
                            quoteIndexBuffer[quoteCount] = i;
                            quoteCount++;
                            if (quoteCount >= 4)
                                break;
                        }  // Find quotes
                    }
                    if (quoteCount < 3) break;

                    string key = line.Substring(quoteIndexBuffer[0] + 1, (quoteIndexBuffer[1] - quoteIndexBuffer[0] - 1));
                    string value = line.Substring(quoteIndexBuffer[2] + 1, (quoteIndexBuffer[3] - quoteIndexBuffer[2] - 1));

                    if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) break;

                    ent.values[key] = value;
                }

                entities.Add(ent);
            }

            nodes = new dnode_t[header.Lumps[5].FileLen / 32];
            reader.ReadArray(ref nodes, header.Lumps[5].FileOfs);

            leafs = new dleaf_t[header.Lumps[10].FileLen / 32];
            reader.ReadArray(ref leafs, header.Lumps[10].FileOfs);

            leafFaces = new ushort[header.Lumps[16].FileLen / 2];
            reader.ReadArray(ref leafFaces, header.Lumps[16].FileOfs);
        }

    }
    public class BspEntity
    {
        public Dictionary<string, string> values = new();
        public bool TryGetTransformedVector3(string key, out Vector3 unityPosition)
        {
            if (values.TryGetValue(key, out string posString) && Conversions.TryParseVector3(posString, out unityPosition))
            {
                unityPosition = Converters.Converter.SourceTransformPointHammer(unityPosition);
                return true;
            }

            unityPosition = default;
            return false;
        }
    }
}
