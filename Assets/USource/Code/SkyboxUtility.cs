using UnityEngine;
using USource.Converters;

namespace USource
{
    public class SkyboxUtility
    {
        public static bool CreateUnitySkyMaterial(string skyName, ImportMode mode, out Material unityMaterial)
        {
            /*
             * create paths from current location path
             */

            // create skybox material
            UnityEngine.Material skyboxMaterial = new UnityEngine.Material(Shader.Find("Skybox/6 Sided"));
            skyboxMaterial.name = skyName;
            skyboxMaterial.SetColor("_Tint", Color.white);

            bool DoSkyStuff(string sourceSide, string unitySide)
            {
                Location sideLocation = new Location($"materials/skybox/{skyName}{sourceSide}.vmt", Location.Type.Source);
                if (USource.ResourceManager.GetUnityObject(sideLocation, out UnityEngine.Material skySideMaterial, mode, true))
                {
                    skyboxMaterial.SetTexture(unitySide, skySideMaterial.mainTexture);
                    return true;
                }
                else
                {
                    return false;
                }

            }

            unityMaterial = skyboxMaterial;

            if (!DoSkyStuff("lf", "_BackTex")) return false;
            if (!DoSkyStuff("rt", "_FrontTex")) return false;
            if (!DoSkyStuff("dn", "_DownTex")) return false;
            if (!DoSkyStuff("up", "_UpTex")) return false;
            if (!DoSkyStuff("ft", "_LeftTex")) return false;
            if (!DoSkyStuff("bk", "_RightTex")) return false;

            return true;
        }
#if UNITY_EDITOR
        public static void SaveUnitySkyMaterial(string skyName, Material unityMaterial)
        {
            string path = $"{USource.baseAssetsPath}UnitySkyboxes/{skyName}.asset";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            UnityEditor.AssetDatabase.CreateAsset(unityMaterial, path);
        }
#endif
    }
}
