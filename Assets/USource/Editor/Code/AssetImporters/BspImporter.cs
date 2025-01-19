using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AssetImporters;
using USource.Converters;

namespace USource.AssetImporters
{
    [ScriptedImporter(0, "bsp")]
    public class BspImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase);
            BspConverter converter = new BspConverter(location.SourcePath, System.IO.File.OpenRead(ctx.assetPath));
            UnityEngine.Object obj = converter.CreateAsset(new ImportContext( ImportMode.AssetDatabase, ctx));
            ctx.AddObjectToAsset("go", obj);
            ctx.SetMainObject(obj);
        }
    }
}
