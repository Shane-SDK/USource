using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AssetImporters;
using USource.Converters;
using USource.SourceAsset;
using static USource.Converters.BspConverter;

namespace USource.AssetImporters
{
    [ScriptedImporter(0, "bsp")]
    public class BspImporter : ScriptedImporter
    {
        public BspConverter.ImportOptions importOptions = new BspConverter.ImportOptions {
            setupDependencies = false,
            splitWorldGeometry = true,
            importWorldColliders = true,
            probeMode = BspConverter.LightProbeMode.GenerateUnityProbes,
            objects = ObjectFlags.LightProbes | ObjectFlags.Props | ObjectFlags.StaticWorld | ObjectFlags.Lights | ObjectFlags.BrushModels | ObjectFlags.Displacements,
            skyboxMode = SkyboxMode.Scale
        };
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase);
            using (System.IO.FileStream file = System.IO.File.OpenRead(ctx.assetPath))
            {
                if (importOptions.setupDependencies)
                {
                    ISourceAsset bspAsset = ISourceAsset.FromLocation(location);
                    DependencyTree tree = new(location);
                    bspAsset.GetDependencies(file, tree, false, ImportMode.CreateAndCache);
                    foreach (Location child in tree.GetImmediateChildren(false))
                    {
                        ctx.DependsOnArtifact(child.AssetPath);
                    }

                    file.Position = 0;
                }

                BspConverter converter = new BspConverter(file, importOptions);
                UnityEngine.Object obj = converter.CreateAsset(new ImportContext(ImportMode.AssetDatabase, ctx));
                ctx.AddObjectToAsset("go", obj);
                ctx.SetMainObject(obj);
            }
        }
    }
}
