using UnityEngine;
using static UnityEditor.FilePathAttribute;
using USource.Converters;

namespace USource.Editor
{
    public class SkyboxUtility
    {
        public static bool CreateUnitySkyMaterial(Location skyMaterialLocation, ImportMode mode, out Material unityMaterial)
        {
            /*
             * create paths from current location path
             */

            // create skybox material
            UnityEngine.Material skyboxMaterial = new UnityEngine.Material(Shader.Find("Skybox/6 Sided"));
            skyboxMaterial.name = skyMaterialLocation.SourcePath;

            bool DoSkyStuff(string sourceSide, string unitySide)
            {
                Location sideLocation = new Location(skyMaterialLocation.SourcePath.Replace("ft.vmt", sourceSide), Location.Type.Source);
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

            if (!DoSkyStuff("lf.vmt", "_BackTex")) return false;
            if (!DoSkyStuff("rt.vmt", "_FrontTex")) return false;
            if (!DoSkyStuff("dn.vmt", "_DownTex")) return false;
            if (!DoSkyStuff("up.vmt", "_UpTex")) return false;
            if (!DoSkyStuff("ft.vmt", "_LeftTex")) return false;
            if (!DoSkyStuff("bk.vmt", "_RightTex")) return false;

            return true;
        }
        public static void SaveUnitySkyMaterial(Location skyLocation, Material unityMaterial)
        {

        }
    }
}
