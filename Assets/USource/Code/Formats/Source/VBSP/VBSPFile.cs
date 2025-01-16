using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using USource.Formats.Source.VTF;
using USource.MathLib;
using UnityEngine;

namespace USource.Formats.Source.VBSP
{
    // https://developer.valvesoftware.com/wiki/Source_BSP_File_Format#Lump_structure
    public class VBSPFile : VBSPStruct
    {
        // ======== BSP ======= //
        public UReader BSPFileReader;
        public dheader_t BSP_Header;

        public dface_t[] BSP_Faces;
        public dmodel_t[] BSP_Models;

        public doverlay_t[] BSP_Overlays;

        public ddispinfo_t[] BSP_DispInfo;
        public dDispVert[] BSP_DispVerts;

        public texinfo_t[] BSP_TexInfo;
        public dtexdata_t[] BSP_TexData;
        public String[] BSP_TextureStringData;

        public dedge_t[] BSP_Edges;
        public Vector3[] BSP_Vertices;
        public Int32[] BSP_Surfedges;

        // ======== OTHER ======= //

        public Face[] BSP_CFaces, BSP_CDisp;
        public string name;

        public PAKProvider pakProvider = null;
        //TODO: Check if LUMPs has a LZMA compression (ex: updated tf maps)
        public static VBSPFile Load(Stream stream, string BSPName = default)
        {
            VBSPFile file = new VBSPFile();
            //bspPath = uLoader.RootPath + "/" + uLoader.ModFolders[0] + "/maps/" + BSPName + ".bsp";
            //if (!File.Exists(bspPath))
            //    throw new FileNotFoundException(String.Format("Map file ({0}) wasn't found in the ({1}) mod-folder. Check weather a path is valid.", BSPName, uLoader.ModFolders[0]));
            file.name = BSPName;
            //bspPath = BSPName;
            file.BSPFileReader = new UReader(stream);
            file.BSPFileReader.ReadType(ref file.BSP_Header);

            if (file.BSP_Header.Ident != 0x50534256)
                throw new FileLoadException(String.Format("{0}: File signature does not match 'VBSP'", BSPName));

            if (file.BSP_Header.Version < 19 || file.BSP_Header.Version > 21)
                throw new FileLoadException(String.Format("{0}: BSP version ({1}) isn't supported", BSPName, file.BSP_Header.Version));

            if (file.BSP_Header.Lumps[0].FileOfs == 0)
            {
                Debug.Log("Found Left 4 Dead 2 header");
                for (Int32 i = 0; i < file.BSP_Header.Lumps.Length; i++)
                {
                    file.BSP_Header.Lumps[i].FileOfs = file.BSP_Header.Lumps[i].FileLen;
                    file.BSP_Header.Lumps[i].FileLen = file.BSP_Header.Lumps[i].Version;
                }
            }

            //BSP_WorldSpawn = new GameObject(BSPName);

            if (file.BSP_Header.Lumps[58].FileLen / 56 <= 0)
            {
                file.BSP_Faces = new dface_t[file.BSP_Header.Lumps[7].FileLen / 56];
                file.BSPFileReader.ReadArray(ref file.BSP_Faces, file.BSP_Header.Lumps[7].FileOfs);
            }
            else
            {
                file.BSP_Faces = new dface_t[file.BSP_Header.Lumps[58].FileLen / 56];
                file.BSPFileReader.ReadArray(ref file.BSP_Faces, file.BSP_Header.Lumps[58].FileOfs);
            }

            file.BSP_Models = new dmodel_t[file.BSP_Header.Lumps[14].FileLen / 48];
            file.BSPFileReader.ReadArray(ref file.BSP_Models, file.BSP_Header.Lumps[14].FileOfs);

            file.BSP_Overlays = new doverlay_t[file.BSP_Header.Lumps[45].FileLen / 352];
            file.BSPFileReader.ReadArray(ref file.BSP_Overlays, file.BSP_Header.Lumps[45].FileOfs);

            file.BSP_DispInfo = new ddispinfo_t[file.BSP_Header.Lumps[26].FileLen / 176];
            file.BSPFileReader.ReadArray(ref file.BSP_DispInfo, file.BSP_Header.Lumps[26].FileOfs);

            file.BSP_DispVerts = new dDispVert[file.BSP_Header.Lumps[33].FileLen / 20];
            file.BSPFileReader.ReadArray(ref file.BSP_DispVerts, file.BSP_Header.Lumps[33].FileOfs);

            file.BSP_TexInfo = new texinfo_t[file.BSP_Header.Lumps[18].FileLen / 12];
            file.BSPFileReader.ReadArray(ref file.BSP_TexInfo, file.BSP_Header.Lumps[18].FileOfs);

            file.BSP_TexInfo = new texinfo_t[file.BSP_Header.Lumps[6].FileLen / 72];
            file.BSPFileReader.ReadArray(ref file.BSP_TexInfo, file.BSP_Header.Lumps[6].FileOfs);

            file.BSP_TexData = new dtexdata_t[file.BSP_Header.Lumps[2].FileLen / 32];
            file.BSPFileReader.ReadArray(ref file.BSP_TexData, file.BSP_Header.Lumps[2].FileOfs);

            file.BSP_TextureStringData = new String[file.BSP_Header.Lumps[44].FileLen / 4];

            Int32[] BSP_TextureStringTable = new Int32[file.BSP_Header.Lumps[44].FileLen / 4];
            file.BSPFileReader.ReadArray(ref BSP_TextureStringTable, file.BSP_Header.Lumps[44].FileOfs);

            //if (ULoader.ModFolders[0] == "tf")
            //    return file;

            for (Int32 i = 0; i < BSP_TextureStringTable.Length; i++)
                file.BSP_TextureStringData[i] = file.BSPFileReader.ReadNullTerminatedString(file.BSP_Header.Lumps[43].FileOfs + BSP_TextureStringTable[i]);

            file.BSP_Edges = new dedge_t[file.BSP_Header.Lumps[12].FileLen / 4];
            file.BSPFileReader.ReadArray(ref file.BSP_Edges, file.BSP_Header.Lumps[12].FileOfs);

            file.BSPFileReader.BaseStream.Seek(file.BSP_Header.Lumps[3].FileOfs, SeekOrigin.Begin);
            file.BSP_Vertices = new Vector3[file.BSP_Header.Lumps[3].FileLen / 12];

            for (Int32 i = 0; i < file.BSP_Vertices.Length; i++)
                file.BSP_Vertices[i] = file.BSPFileReader.ReadVector3D(true) * USource.settings.sourceToUnityScale;

            file.BSP_Surfedges = new Int32[file.BSP_Header.Lumps[13].FileLen / 4];
            file.BSPFileReader.ReadArray(ref file.BSP_Surfedges, file.BSP_Header.Lumps[13].FileOfs);

            //file.BSP_Brushes = new List<GameObject>();

            //Create new lightmap list
            //ULoader.lightmapsData = new List<LightmapData>();
            //LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

            return file;
        }
    }
}
