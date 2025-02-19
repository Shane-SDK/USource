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
        public Dictionary<Converters.MaterialConverter.Map, Location> maps;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using Stream stream = File.OpenRead(ctx.assetPath);
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase, null);
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            DependencyTree dependencies = new(location);
            sourceAsset.GetDependencies(stream, dependencies, false);

            foreach (Location dependency in dependencies.GetImmediateChildren(false))
                ctx.DependsOnArtifact(dependency.AssetPath);

            stream.Position = 0;

            Converters.MaterialConverter materialConverter = new Converters.MaterialConverter(stream);
            flags = materialConverter.flags;
            maps = new();
            foreach (KeyValuePair<Converters.MaterialConverter.Map, Location> pair in materialConverter.Maps)
            {
                maps.Add(pair.Key, pair.Value);
            }

            UnityEngine.Material obj = materialConverter.CreateAsset(new ImportContext(ImportMode.AssetDatabase, ctx)) as UnityEngine.Material;
            ctx.AddObjectToAsset("material", obj);
            ctx.SetMainObject(obj);
        }
    }
}
