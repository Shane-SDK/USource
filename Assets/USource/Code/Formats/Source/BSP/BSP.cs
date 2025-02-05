using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.VisualScripting.YamlDotNet.Core;
using UnityEngine;
using USource.Formats.Source.PHYS;
using USource.SourceAsset;

namespace USource.Formats.Source.BSP
{
    // https://developer.valvesoftware.com/wiki/Source_BSP_File_Format#Lump_structure
    public class BSP
    {
        public int Version => header.version;
        // ======== BSP ======= //
        public UReader reader;
        public Header header;
        public Face[] faces;
        public BrushModel[] models;
        public Overlay[] overlays;
        public DisplacementInfo[] dispInfo;
        public DisplacementVertex[] dispVerts;
        public TextureInfo[] texInfo;
        public TextureData[] texData;
        public string[] textureStringData;
        public Edge[] edges;
        public Vector3[] vertices;
        public int[] surfEdges;
        public Node[] nodes;
        public Leaf[] leafs;
        public ushort[] leafFaces;
        public GameLumpHeader[] gameLumps;
        public Plane[] planes;
        public string[] staticPropDict;
        public ushort[] staticPropLeafEntries;
        public StaticPropLump_t[] staticPropLumps;
        public LeafAmbientIndex[] ldrAmbientIndices;
        public LeafAmbientLighting[] ldrAmbientLighting;
        public List<PhysicsBrushModel> physModelHeaders;
        public Dictionary<int, PhysModel> physModels;
        public List<BspEntity> entities;
        public PAKProvider pakProvider = null;
        public BSP(Stream stream, string BSPName = default)
        {
            reader = new UReader(stream);
            header.ReadToObject(reader, Version);

            if (header.identity != 0x50534256)
                throw new FileLoadException(String.Format("{0}: File signature does not match 'VBSP'", BSPName));

            if (header.version < 19 || header.version > 21)
                throw new FileLoadException(String.Format("{0}: BSP version ({1}) isn't supported", BSPName, header.version));

            if (header.lumps[58].fileLength / 56 <= 0)
            {
                faces = new Face[header.lumps[7].fileLength / 56];
                reader.ReadSourceObjectArray(ref faces, header.lumps[7].fileOffset, Version);
            }
            else
            {
                faces = new Face[header.lumps[58].fileLength / 56];
                reader.ReadSourceObjectArray(ref faces, header.lumps[58].fileOffset, Version);
            }

            models = new BrushModel[header.lumps[14].fileLength / 48];
            reader.ReadSourceObjectArray(ref models, header.lumps[14].fileOffset, Version);

            overlays = new Overlay[header.lumps[45].fileLength / 352];
            reader.ReadSourceObjectArray(ref overlays, header.lumps[45].fileOffset, Version);

            dispInfo = new DisplacementInfo[header.lumps[26].fileLength / 176];
            reader.ReadSourceObjectArray(ref dispInfo, header.lumps[26].fileOffset, Version);

            dispVerts = new DisplacementVertex[header.lumps[33].fileLength / 20];
            reader.ReadSourceObjectArray(ref dispVerts, header.lumps[33].fileOffset, Version);

            texInfo = new TextureInfo[header.lumps[18].fileLength / 12];
            reader.ReadSourceObjectArray(ref texInfo, header.lumps[18].fileOffset, Version);

            texInfo = new TextureInfo[header.lumps[6].fileLength / 72];
            reader.ReadSourceObjectArray(ref texInfo, header.lumps[6].fileOffset, Version);

            texData = new TextureData[header.lumps[2].fileLength / 32];
            reader.ReadSourceObjectArray(ref texData, header.lumps[2].fileOffset, Version);

            planes = new Plane[header.lumps[1].fileLength / 20];
            reader.ReadSourceObjectArray(ref planes, header.lumps[1].fileOffset, Version);

            ldrAmbientIndices = new LeafAmbientIndex[header.lumps[52].fileLength / 4];
            reader.ReadSourceObjectArray(ref ldrAmbientIndices, header.lumps[52].fileOffset, Version);

            ldrAmbientLighting = new LeafAmbientLighting[header.lumps[56].fileLength / 28];
            reader.ReadSourceObjectArray(ref ldrAmbientLighting, header.lumps[56].fileOffset, Version);

            textureStringData = new String[header.lumps[44].fileLength / 4];

            int[] BSP_TextureStringTable = new Int32[header.lumps[44].fileLength / 4];
            reader.ReadArray(ref BSP_TextureStringTable, header.lumps[44].fileOffset);

            //if (ULoader.ModFolders[0] == "tf")
            //    return file;

            for (Int32 i = 0; i < BSP_TextureStringTable.Length; i++)
                textureStringData[i] = reader.ReadNullTerminatedString(header.lumps[43].fileOffset + BSP_TextureStringTable[i]);

            // Replace patched materials
            for (int i = 0; i < textureStringData.Length; i++)
            {
                if (ISourceAsset.TryResolvePatchMaterial(new Location($"materials/{textureStringData[i]}.vmt", Location.Type.Source), out Location patchedMaterial))
                {
                    int startIndex = 9;
                    int endIndex = patchedMaterial.SourcePath.Length - 4;
                    textureStringData[i] = patchedMaterial.SourcePath.Substring(9, endIndex - startIndex);
                }
            }

            edges = new Edge[header.lumps[12].fileLength / 4];
            reader.ReadSourceObjectArray(ref edges, header.lumps[12].fileOffset, Version);

            reader.BaseStream.Seek(header.lumps[3].fileOffset, SeekOrigin.Begin);
            vertices = new Vector3[header.lumps[3].fileLength / 12];

            for (Int32 i = 0; i < vertices.Length; i++)
                vertices[i] = reader.ReadVector3D(true) * USource.settings.sourceToUnityScale;

            surfEdges = new int[header.lumps[13].fileLength / 4];
            reader.ReadArray(ref surfEdges, header.lumps[13].fileOffset);

            entities = new();
            reader.BaseStream.Seek(header.lumps[0].fileOffset, SeekOrigin.Begin);
            MatchCollection Matches = Regex.Matches(
                new(reader.ReadChars(header.lumps[0].fileLength)),
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

            nodes = new Node[header.lumps[5].fileLength / 32];
            reader.ReadSourceObjectArray(ref nodes, header.lumps[5].fileOffset, Version);

            leafs = new Leaf[header.lumps[10].fileLength / 32];
            reader.ReadSourceObjectArray(ref leafs, header.lumps[10].fileOffset, Version);

            leafFaces = new ushort[header.lumps[16].fileLength / 2];
            reader.ReadArray(ref leafFaces, header.lumps[16].fileOffset);

            reader.BaseStream.Seek(header.lumps[35].fileOffset, SeekOrigin.Begin);
            gameLumps = new GameLumpHeader[reader.ReadInt32()];
            reader.ReadSourceObjectArray(ref gameLumps, header.lumps[35].fileOffset + 4, Version);

            staticPropLumps = new StaticPropLump_t[0];
            for (int i = 0; i < gameLumps.Length; i++)
            {
                if (gameLumps[i].id == 1936749168)  // Static prop dictionary
                {
                    reader.BaseStream.Seek(gameLumps[i].fileOffset, SeekOrigin.Begin);
                    staticPropDict = new String[reader.ReadInt32()];
                    for (Int32 j = 0; j < staticPropDict.Length; j++)
                    {
                        staticPropDict[j] = new String(reader.ReadChars(128));

                        if (staticPropDict[j].Contains('\0'))
                            staticPropDict[j] = staticPropDict[j].Split('\0')[0];
                    }

                    staticPropLeafEntries = new ushort[reader.ReadInt32()];
                    reader.ReadArray(ref staticPropLeafEntries);

                    long nStaticProps = reader.ReadInt32();
                    if (nStaticProps == 0) continue;
                    staticPropLumps = new StaticPropLump_t[nStaticProps];
                    long staticPropSize = (gameLumps[i].fileLength - (reader.BaseStream.Position - gameLumps[i].fileOffset)) / nStaticProps;
                    long staticPropLumpStart = reader.BaseStream.Position;

                    for (long l = 0; l < nStaticProps; l++)
                    {
                        ushort pathIndex;
                        Vector3 m_Origin;
                        Vector3 m_Angles;

                        reader.BaseStream.Position = staticPropLumpStart + l * staticPropSize;
                        switch (gameLumps[i].version)
                        {
                            case 11:
                                StaticPropLumpV11_t StaticPropLumpV11_t = new StaticPropLumpV11_t();
                                reader.ReadType(ref StaticPropLumpV11_t);

                                pathIndex = StaticPropLumpV11_t.m_PropType;
                                m_Origin = Converters.IConverter.SourceTransformPointHammer(StaticPropLumpV11_t.m_Origin);
                                m_Angles = Converters.IConverter.SourceTransformAnglesHammer(StaticPropLumpV11_t.m_Angles);

                                break;
                            case 600:
                                StaticPropLumpV6_t propLump = new StaticPropLumpV6_t();
                                reader.ReadType(ref propLump);

                                pathIndex = propLump.m_PropType;
                                m_Origin = Converters.IConverter.SourceTransformPointHammer(propLump.m_Origin);
                                m_Angles = Converters.IConverter.SourceTransformAnglesHammer(propLump.m_Angles);

                                break;
                            default:
                                StaticPropLumpV4_t StaticPropLumpV4_t = new StaticPropLumpV4_t();
                                reader.ReadType(ref StaticPropLumpV4_t);

                                pathIndex = StaticPropLumpV4_t.m_PropType;
                                m_Origin = Converters.IConverter.SourceTransformPointHammer(StaticPropLumpV4_t.m_Origin);
                                m_Angles = Converters.IConverter.SourceTransformAnglesHammer(StaticPropLumpV4_t.m_Angles);
                                break;
                        }

                        staticPropLumps[l] = new StaticPropLump_t { Angles = m_Angles, Origin = m_Origin, PropType = pathIndex };
                    }
                }
            }

            reader.BaseStream.Position = header.lumps[29].fileOffset;
            physModels = new();
            physModelHeaders = new();
            int iteration = 0;
            while (true)
            {
                if (iteration > 100) break;
                iteration++;

                PhysModelHeader modelHeader = new();
                modelHeader.ReadToObject(reader);
                if (modelHeader.modelIndex == -1) break;  // lump is terminated by this

                //Debug.Log($"BSP Phys Model {iteration - 1}, {reader.BaseStream.Position.ToString("X")}");

                long currentPos = reader.BaseStream.Position;
                CollisionData[] solids = new CollisionData[modelHeader.solidCount];
                physModels[modelHeader.modelIndex] = new PhysModel { solids = solids, modelIndex = modelHeader.modelIndex };
                for (int i = 0; i < modelHeader.solidCount; i++)
                {
                    //Debug.Log($"Collision Data {i}, {reader.BaseStream.Position.ToString("X")}");
                    solids[i] = new PHYS.CollisionData(modelHeader.dataSize);
                    solids[i].ReadToObject(reader);
                }

                reader.BaseStream.Position = currentPos + modelHeader.dataSize + modelHeader.keyDataSize;
                // key data
            }
        }
    }
    /// <summary>
    /// BSP Phys Lump header for each model
    /// </summary>
    public struct PhysModelHeader : ISourceObject
    {
        public int modelIndex;
        public int dataSize;
        public int keyDataSize;
        public int solidCount;
        public void ReadToObject(UReader reader, int version = 0)
        {
            modelIndex = reader.ReadInt32();
            dataSize = reader.ReadInt32();
            keyDataSize = reader.ReadInt32();
            solidCount = reader.ReadInt32();
        }
    }
    public class BspEntity
    {
        public Dictionary<string, string> values = new();
        public bool TryGetTransformedVector3(string key, out Vector3 unityPosition)
        {
            if (values.TryGetValue(key, out string posString) && Conversions.TryParseVector3(posString, out unityPosition))
            {
                unityPosition = Converters.IConverter.SourceTransformPointHammer(unityPosition);
                return true;
            }

            unityPosition = default;
            return false;
        }
        public bool TryGetVector4(string key, out Vector4 vector)
        {
            if (values.TryGetValue(key, out string posString) && Conversions.TryParseVector4(posString, out vector))
            {
                return true;
            }

            vector = default;
            return false;
        }
        public bool TryGetValue(string key, out string value)
        {
            return values.TryGetValue(key, out value);
        }
        public bool TryGetFloat(string key, out float value)
        {
            value = default;
            return TryGetValue(key, out string stringFloat) && float.TryParse(stringFloat, out value);
        }
    }
    public class PhysModel
    {
        public int modelIndex;
        public CollisionData[] solids;
    }
}
