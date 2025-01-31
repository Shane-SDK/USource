using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AssetImporters;
using USource.Converters;
using USource.SourceAsset;

namespace USource.AssetImporters
{
    [ScriptedImporter(0, "bsp")]
    public class BspImporter : ScriptedImporter
    {
        public BspConverter.ImportOptions importOptions = new BspConverter.ImportOptions { cullSkybox = true, setupDependencies = false, splitWorldGeometry = true };
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase);
            BspConverter converter = new BspConverter(System.IO.File.OpenRead(ctx.assetPath), importOptions);

            if (importOptions.setupDependencies)
            {
                ISourceAsset bspAsset = ISourceAsset.FromLocation(location);
                DependencyTree tree = new(location);
                bspAsset.GetDependencies(System.IO.File.OpenRead(ctx.assetPath), tree, false, ImportMode.CreateAndCache);
                foreach (Location child in tree.GetImmediateChildren(false))
                {
                    ctx.DependsOnArtifact(child.AssetPath);
                }
            }

            UnityEngine.Object obj = converter.CreateAsset(new ImportContext( ImportMode.AssetDatabase, ctx));
            ctx.AddObjectToAsset("go", obj);
            ctx.SetMainObject(obj);
        }
    }
}
