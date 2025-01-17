using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

namespace USource.Converters
{
    public class Material : Converter
    {
        public enum Map
        {
            Diffuse,
            Bump,
            Transparency,
            Illumination,
            Environment,
        }
        public IReadOnlyDictionary<Map, Location> Maps => maps;
        Formats.Source.VTF.VMTFile vmt;
        Dictionary<Map, Location> maps;
        public MaterialFlags flags;
        public Material(string sourcePath, System.IO.Stream stream) : base(sourcePath, stream)
        {
            this.vmt = new Formats.Source.VTF.VMTFile(stream, sourcePath);

            maps = new();

            if (vmt.TryGetValue("$basetexture", out string value))
                maps[Map.Diffuse] = new Location($"materials/{value}.vtf", Location.Type.Source);

            if (vmt.TryGetValue("$bumpmap", out value))
                maps[Map.Bump] = new Location($"materials/{value}.vtf", Location.Type.Source);

            if (vmt.TryGetValue("$translucent", out value))
                maps[Map.Transparency] = new Location($"materials/{value}.vtf", Location.Type.Source);

            if (vmt.TryGetValue("$selfillummask", out value))
                maps[Map.Illumination] = new Location($"materials/{value}.vtf", Location.Type.Source);

            if (vmt.TryGetValue("$envmapmask", out value))
                maps[Map.Environment] = new Location($"materials/{value}.vtf", Location.Type.Source);

            if (vmt.ContainsParma("%CompileNoDraw"))
                flags |= MaterialFlags.Invisible | MaterialFlags.NonSolid | MaterialFlags.NoShadows;

            if (vmt.ContainsParma("%CompileNonSolid"))
                flags |= MaterialFlags.NonSolid;

            if (vmt.ContainsParma("%CompileWater"))
                flags |= MaterialFlags.NonSolid;

            if (vmt.ContainsParma("%CompileTrigger"))
                flags |= MaterialFlags.Invisible | MaterialFlags.NoShadows | MaterialFlags.NonSolid;

            if (vmt.ContainsParma("%CompileSky") || vmt.ContainsParma("%Compile2DSky"))
                flags |= MaterialFlags.Invisible | MaterialFlags.NoShadows | MaterialFlags.NonSolid;

            if (vmt.ContainsParma("%CompileClip") || vmt.ContainsParma("%CompileInvisible") || vmt.ContainsParma("%PlayerClip"))
                flags |= MaterialFlags.Invisible | MaterialFlags.NoShadows | MaterialFlags.NonSolid;

            if (vmt.ContainsParma("%CompileSkip") || vmt.ContainsParma("%CompileHint") || vmt.ContainsParma("%CompileLadder"))
                flags |= MaterialFlags.Invisible | MaterialFlags.NoShadows | MaterialFlags.NonSolid;
        }
        public override UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            // Check if this is including a material and just return that
            //if (vmt.TryGetValue("include", out string includedMaterialPath))
            //{
            //    if (ResourceManager.TryImportAsset<UnityEngine.Material>(new Location(includedMaterialPath, Location.Type.Source), out UnityEngine.Material includedAsset))
            //    {
            //        return includedAsset;
            //    }
            //}

            // Create a new material
            //UnityEngine.Shader shader = GetShader(vmt.shaderKey);

            UnityEngine.Shader shader = null;

            {
                string[] guids = AssetDatabase.FindAssets("t:Shader USource", new[] { "Assets/USource" });
                if (guids.Length == 0)
                {
                    Debug.LogError("Could not find USource shader");
                    return null;
                }

                shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            UnityEngine.Material material = new UnityEngine.Material(shader);
            material.doubleSidedGI = true;
            material.name = vmt.FileName;

            if (vmt.TryGetValue("$basetexture", out string value))
            {
                string texturePath = $"materials/{value}.vtf";
                Location location = new Location(texturePath, Location.Type.Source);
                if (USource.ResourceManager.GetUnityObject<Texture2D>(location, out Texture2D texture, ctx.ImportMode))
                    material.mainTexture = texture;

            }

            if (vmt.TryGetValue("$bumpmap", out string bumpString))
            {
                if (USource.ResourceManager.GetUnityObject<Texture2D>(new Location($"materials/{bumpString}.vtf", Location.Type.Source), out Texture2D texture, ctx.ImportMode))
                    material.SetTexture("_bumpMap", texture);
            }

            // https://forum.unity.com/threads/change-standard-shader-render-mode-in-runtime.318815/

            if (vmt.TryGetValue("$alphatest", out float alphaTestValue) && alphaTestValue == 1)
            {
                material.SetFloat("_AlphaClip", 1);
                material.SetFloat("_Surface", 0);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                //flags |= AssetInfo.MaterialFlags.Nonsolid;
            }

            if (vmt.TryGetValue("$translucent", out float translucent) && translucent == 1)
            {
                material.SetFloat("_Surface", 1);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2449;
                //flags |= AssetInfo.MaterialFlags.Nonsolid;
            }

            if (vmt.TryGetValue("$envmap", out float _) == false)
            {
                material.SetFloat("_EnvironmentReflections", 0);
                material.SetFloat("_SpecularHighlights", 0);
                material.SetFloat("_Smoothness", 0);
                material.SetFloat("_Metallic", 0);
            }

            if (vmt.TryGetValue("$envmaptint", out UnityEngine.Vector3 tint))
            {
                // Average values for metallic/specular intensity
                //float average = (tint.x + tint.y + tint.z) / 3.0f;
                //material.SetFloat("_Smoothness", average);
                material.SetVector("_envmapTint", tint);
            }

            if (vmt.TryGetValue("$selfillum", out float _))
            {
                material.SetInt("Emissive", 1);
            }

            if (vmt.TryGetValue("$selfillummask", out string illumMask) && USource.ResourceManager.GetUnityObjectFromCache(new Location(illumMask, Location.Type.Source), out Texture2D illumMaskTexture))
            {
                material.SetTexture("EmissiveMask", illumMaskTexture);
            }

            //if (vmt.TryGetValue("$basealphaenvmapmask", out string v))
            //    UnityEngine.Debug.Log(v);

            if (vmt.TryGetValue("$envmapmask", out string envmapMaskPath))
            {
                envmapMaskPath = $"materials/{envmapMaskPath}.vtf";
                if (USource.ResourceManager.GetUnityObjectFromCache(new Location(envmapMaskPath, Location.Type.Source), out UnityEngine.Texture texture))
                {
                    //material.SetTexture("_SpecGlossMap", texture);
                    //material.SetTexture("_MetallicGlossMap", texture);
                    material.SetTexture("_envmapMask", texture);
                }
            }

            if (vmt.TryGetValue("$basealphaenvmapmask", out float _))
            {
                material.SetFloat("_envmapMaskBaseAlpha", 1);
            }

            unityObject = material;

            return material;
        }
        public static UnityEngine.Shader GetShader(string sourceName)
        {
            switch (sourceName)
            {
                default:
                    return UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
                    //return Shader.Find("Standard");
            }
        }
    }
}
