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
            Stream stream = File.OpenRead(ctx.assetPath);
            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase, null);
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            DependencyTree dependencies = new(location);
            sourceAsset.GetDependencies(stream, dependencies, false);

            foreach (Location dependency in dependencies.GetImmediateChildren(false))
                ctx.DependsOnArtifact(dependency.AssetPath);

            stream.Close();
            stream = File.OpenRead(ctx.assetPath);

            Converters.MaterialConverter materialConverter = new Converters.MaterialConverter(location.SourcePath, stream);
            flags = materialConverter.flags;
            maps = new();
            foreach (KeyValuePair<Converters.MaterialConverter.Map, Location> pair in materialConverter.Maps)
            {
                maps.Add(pair.Key, pair.Value);

                if (pair.Key == Converters.MaterialConverter.Map.Bump)
                {
                    if (AssetImporter.GetAtPath(pair.Value.AssetPath) is VtfImporter vtfImporter)
                    {
                        if (vtfImporter.normalMap == false)
                        {
                            vtfImporter.normalMap = true;
                            EditorUtility.SetDirty(vtfImporter);
                        }
                    }
                }
                else if (pair.Key == Converters.MaterialConverter.Map.Diffuse)
                {
                    if (flags.HasFlag(MaterialFlags.Skybox) && AssetImporter.GetAtPath(pair.Value.AssetPath) is VtfImporter vtfImporter)
                    {
                        vtfImporter.wrapMode = TextureWrapMode.Clamp;
                    }
                }
            }

            UnityEngine.Material obj = materialConverter.CreateAsset( new ImportContext(ImportMode.AssetDatabase, ctx) ) as UnityEngine.Material;
            ctx.AddObjectToAsset("material", obj);
            ctx.SetMainObject(obj);

            if (location.SourcePath.Contains("/skybox/") && location.SourcePath.Contains("ft.vmt"))
            {
                // create skybox material
                UnityEngine.Material skyboxMaterial = new UnityEngine.Material(Shader.Find("Skybox/6 Sided"));
                skyboxMaterial.name = "skybox";

                void DoSkyStuff(string sourceSide, string unitySide)
                {
                    if (sourceSide == "ft.vmt")
                    {
                        obj.mainTexture.wrapMode = TextureWrapMode.Clamp;
                        skyboxMaterial.SetTexture(unitySide, obj.mainTexture);
                    }
                    else if (USource.ResourceManager.GetUnityObject(new Location(location.SourcePath.Replace("ft.vmt", sourceSide), Location.Type.Source), out UnityEngine.Material skySideMaterial, ImportMode.AssetDatabase, true))
                    {
                        // Update texture importer to clamp texture
                        skySideMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
                        //VtfImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texResource.mainTexture)) as VtfImporter;
                        //textureImporter.wrapMode = TextureWrapMode.Clamp;
                        skyboxMaterial.SetTexture(unitySide, skySideMaterial.mainTexture);
                    }
                    
                }

                DoSkyStuff("lf.vmt", "_BackTex");
                DoSkyStuff("rt.vmt", "_FrontTex");
                DoSkyStuff("dn.vmt", "_DownTex");
                DoSkyStuff("up.vmt", "_UpTex");
                DoSkyStuff("ft.vmt", "_LeftTex");
                DoSkyStuff("bk.vmt", "_RightTex");

                ctx.AddObjectToAsset("sky", skyboxMaterial);
            }

            stream?.Close();
        }
    }
}
