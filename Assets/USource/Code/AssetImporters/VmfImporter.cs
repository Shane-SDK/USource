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
            //List<Location> dependencies = new();
            //ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            //sourceAsset.GetDependencies(stream, dependencies);
            //for (int i = dependencies.Count - 1; i >= 1; i--)
            //{
            //    ctx.DependsOnArtifact(dependencies[i].AssetPath);
            //}

            //stream.Close();
            //stream = System.IO.File.OpenRead(ctx.assetPath);

            VmfConverter converter = new VmfConverter(location.SourcePath, stream);
            UnityEngine.Object obj = converter.CreateAsset(ImportMode.AssetDatabase);
            ctx.AddObjectToAsset("level", obj);
            ctx.SetMainObject(obj);
        }
    }
}
