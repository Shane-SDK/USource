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
    [ScriptedImporter(0, "vtf")]
    public class VtfImporter : ScriptedImporter
    {
        public Converters.TextureConverter.ImportOptions options = new TextureConverter.ImportOptions { 
            maxSize = 1024, 
            mipMaps = true, 
            wrapMode = TextureWrapMode.Repeat
        };
        //public TextureFormat textureFormat;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Stream stream = File.OpenRead(ctx.assetPath);

            string sourcePath = USource.AssetPathToSourcePath(ctx.assetPath);

            Converters.TextureConverter texture = new Converters.TextureConverter(stream, options);
            UnityEngine.Texture obj = texture.CreateAsset( new ImportContext(ImportMode.AssetDatabase, ctx) ) as UnityEngine.Texture;
            ctx.AddObjectToAsset("texture", obj);
            ctx.SetMainObject(obj);

            stream?.Close();
        }
    }
}
