using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace USource
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class USource
    {
        public static ResourceManager ResourceManager => resourceManager;
        static ResourceManager resourceManager;
        public static Settings settings;
        public readonly static HashSet<string> noRenderMaterials = new HashSet<string>
        {
            "tools/toolsnodraw",
            "tools/toolsinvisible"
        };
        public static readonly HashSet<string> noCreateMaterials = new HashSet<string>
        {
            "tools/toolsskybox",
            "tools/toolsskybox2d",
            "tools/toolsskip",
            "tools/toolsplayerclip",
            "tools/toolshint",
            "tools/toolsclip",
            "tools/toolstrigger"
        };
        static USource()
        {
            Init();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
#if UNITY_EDITOR
            settings = UnityEditor.AssetDatabase.LoadAssetAtPath<Settings>(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("t:Settings", new[] { "Assets/USource" })[0]));
#endif
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
