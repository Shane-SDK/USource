using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace USource.AssetImporters
{
    public class VmtProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            //foreach (string assetPath in importedAssets.Where<string>(s => { return Path.GetExtension(s) == ".vmt"; }))
            //{
            //    AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
            //    if (assetImporter != null && assetImporter is VmtImporter vmtImporter)
            //    {
            //        if (vmtImporter.maps != null && vmtImporter.maps.TryGetValue(Converters.Material.Map.Bump, out Location location))
            //        {
            //            assetImporter = AssetImporter.GetAtPath(location.AssetPath);
            //            if (assetImporter != null && assetImporter is VtfImporter vtfImporter)
            //            {
            //                vtfImporter.normalMap = true;
            //                //vtfImporter.SaveAndReimport();
            //                //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            //            }
            //        }
            //    }
            //}
        }
    }
}
