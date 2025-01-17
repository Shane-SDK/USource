using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using USource;
using System.IO;
using USource.Converters;
using USource.SourceAsset;
using System.Linq;

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
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase, null);
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            DependencyTree dependencies = new(location);
            sourceAsset.GetDependencies(stream, dependencies, false);

            foreach (Location dependency in dependencies.GetImmediateChildren(false))
                ctx.DependsOnArtifact(dependency.AssetPath);

            stream.Close();
            stream = File.OpenRead(ctx.assetPath);

            Converters.Material materialConverter = new Converters.Material(location.SourcePath, stream);
            flags = materialConverter.flags;
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

            UnityEngine.Material obj = materialConverter.CreateAsset( new ImportContext(ImportMode.AssetDatabase, ctx) ) as UnityEngine.Material;
            ctx.AddObjectToAsset("material", obj);
            ctx.SetMainObject(obj);

            stream?.Close();
        }
    }
}
