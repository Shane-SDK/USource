using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using USource.Converters;

namespace USource.AssetImporters
{
    public class VmtProcessor : AssetPostprocessor
    {
        //private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        //{
        //    foreach (string assetPath in importedAssets.Where<string>(s => { return Path.GetExtension(s) == ".vmt"; }))
        //    {
        //        Location location = new Location(assetPath, Location.Type.AssetDatabase);
        //        if (USource.ResourceManager.GetStream(location, out Stream stream, ImportMode.AssetDatabase))
        //        {
        //            MaterialConverter converter = Converter.FromLocation(location, stream) as MaterialConverter;
        //            if (converter.Maps.TryGetValue( MaterialConverter.Map.Bump, out Location normalLocation))
        //            {
        //                VtfImporter assetImporter = AssetImporter.GetAtPath(location.AssetPath) as VtfImporter;
        //                assetImporter.options.color = TextureConverter.ColorMode.Normal;
        //                EditorUtility.SetDirty(assetImporter);
        //                assetImporter.SaveAndReimport();
        //            }
        //        }
        //    }
        //}
    }
}
