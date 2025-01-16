using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace USource
{
    [InitializeOnLoad]
    public static class USource
    {
        public static ResourceManager ResourceManager => resourceManager;
        static ResourceManager resourceManager;
        public static Settings settings;
        static USource()
        {
            Init();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            settings = AssetDatabase.LoadAssetAtPath<Settings>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:Settings", new[] { "Assets/USource" })[0]));
            resourceManager = new ResourceManager();
        }
        public static Type GetTypeFromExtension(string sourceExtension)
        {
            switch (sourceExtension)
            {
                case "vtf":
                    return typeof(Texture2D);
                case "vmt":
                    return typeof(UnityEngine.Material);
                case "mdl":
                    return typeof(GameObject);
            }

            return null;
        }
        public static string StripExtension(string path)
        {
            int extensionIndex = path.LastIndexOf('.');
            if (extensionIndex == -1)
                return path;

            return path.Remove(extensionIndex, path.Length - extensionIndex);
        }
        public static string GetUnityAssetExtension(System.Type type)
        {
            if (type == typeof(UnityEngine.Material))
                return "mat";
            if (type == typeof(GameObject))
                return "prefab";
            if (type == typeof(Texture2D))
                return "png";

            return "";
        }
        public static string AssetPathToSourcePath(string assetPath)
        {
            string preceedingPath = $"Assets/USource/Assets/";
            return assetPath.Substring(preceedingPath.Length, assetPath.Length - preceedingPath.Length);
        }
        public static string SourcePathToAssetPath(string sourcePath)
        {
            return $"Assets/USource/Assets/{sourcePath}";  // Path of the asset relative to project
        }
    }
}
