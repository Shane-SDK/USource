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
        public bool normalMap;
        [HideInInspector]
        public int maxSize = 1024;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public bool mipmaps = true;
        //public TextureFormat textureFormat;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Stream stream = File.OpenRead(ctx.assetPath);

            string sourcePath = USource.AssetPathToSourcePath(ctx.assetPath);

            Converters.TextureConverter texture = new Converters.TextureConverter(sourcePath, stream, normalMap ? Converters.TextureConverter.ImportOptions.Normal : 0, maxSize);
            texture.mipmaps = mipmaps;
            texture.wrapMode = wrapMode;
            UnityEngine.Texture obj = texture.CreateAsset( new ImportContext(ImportMode.AssetDatabase, ctx) ) as UnityEngine.Texture;
            ctx.AddObjectToAsset("texture", obj);
            ctx.SetMainObject(obj);

            stream?.Close();
        }
    }
}
