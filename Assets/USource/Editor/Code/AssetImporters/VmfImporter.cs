#if RealtimeCSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using USource.Converters;
using USource.SourceAsset;

namespace USource.AssetImporters
{
    [ScriptedImporter(0, "vmf")]
    public class VmfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase);
            System.IO.Stream stream = System.IO.File.OpenRead(ctx.assetPath);
            DependencyTree dependencies = new(location);
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            sourceAsset.GetDependencies(stream, dependencies, false);
            foreach (Location dependency in dependencies.GetImmediateChildren(false))
                ctx.DependsOnArtifact(dependency.AssetPath);

            stream.Close();
            stream = System.IO.File.OpenRead(ctx.assetPath);

            VmfConverter converter = new VmfConverter(stream);
            UnityEngine.Object obj = converter.CreateAsset(new ImportContext(ImportMode.AssetDatabase, ctx));
            ctx.AddObjectToAsset("level", obj);
            ctx.SetMainObject(obj);
        }
    }
}
#endif