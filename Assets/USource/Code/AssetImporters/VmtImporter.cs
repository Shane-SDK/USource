using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using USource;
using System.IO;
using USource.Converters;
namespace USource.AssetImporters
{
    [ScriptedImporter(0, "vmt")]
    public class VmtImporter : ScriptedImporter
    {
        public MaterialFlags flags;
        public Dictionary<Converters.Material.Map, Location> maps;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Stream stream = File.OpenRead(ctx.assetPath);

            string sourcePath = USource.AssetPathToSourcePath(ctx.assetPath);

            Converters.Material materialConverter = new Converters.Material(sourcePath, stream);

            flags = materialConverter.flags;

            foreach (string sourcePathDependency in materialConverter.GetSourceAssetDependencies())
            {
                Location depenLocation = new Location(sourcePathDependency, Location.Type.Source);
                ctx.DependsOnArtifact(depenLocation.AssetPath);
            }

            maps = new();
            foreach (KeyValuePair<Converters.Material.Map, Location> pair in materialConverter.Maps)
            {
                maps.Add(pair.Key, pair.Value);

                if (pair.Key == Converters.Material.Map.Bump)
                {
                    if (AssetImporter.GetAtPath(pair.Value.AssetPath) is VtfImporter vtfImporter)
                    {
                        if (vtfImporter.normalMap == false)
                        {
                            vtfImporter.normalMap = true;
                            EditorUtility.SetDirty(vtfImporter);
                        }
                    }
                }
                else if (pair.Key == Converters.Material.Map.Diffuse)
                {
                    if (flags.HasFlag(MaterialFlags.Skybox) && AssetImporter.GetAtPath(pair.Value.AssetPath) is VtfImporter vtfImporter)
                    {
                        vtfImporter.wrapMode = TextureWrapMode.Clamp;
                    }
                }
            }

            

            UnityEngine.Material obj = materialConverter.CreateAsset() as UnityEngine.Material;
            ctx.AddObjectToAsset("material", obj);
            ctx.SetMainObject(obj);

            stream?.Close();
        }
    }
}
