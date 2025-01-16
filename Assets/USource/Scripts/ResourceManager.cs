using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USource.Converters;
using JetBrains.Annotations;
using UnityEditor.VersionControl;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace USource
{
    public static class ResourceManager
    {
        [System.Flags]
        public enum ImportFlags
        {
            Normal = 1 << 0,
            Skybox = 1 << 1,
            Hitboxes = 1 << 2,
            Animations = 1 << 3,
            Physics = 1 << 4,
            Geometry = 1 << 5,
        }
        public enum ImportMode
        {
            /// <summary>
            /// Load from the Asset database, do not import from Source if not found
            /// </summary>
            ReadFromAssetDataBaseDontImport,
            /// <summary>
            /// Import from Source files and load
            /// </summary>
            ImportAndLoad,
            /// <summary>
            /// Read from the Asset database, will import from Source if not found
            /// </summary>
            ReadFromAssetDataBaseOrImport,
        }
        public static Settings settings;
        public static List<IResourceProvider> ResourceProviders { get { return resourceProviders; } }
        static List<IResourceProvider> resourceProviders = new List<IResourceProvider>();
        public static IReadOnlyDictionary<Location, UnityEngine.Object> ObjectCache => objectCache;
        static Dictionary<Location, UnityEngine.Object> objectCache = new();
        static ResourceManager()
        {
            settings = AssetDatabase.LoadAssetAtPath<Settings>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:Settings", new[] { "Assets/USource" })[0]));
        }
        /// <summary>
        /// Initializes resource providers
        /// </summary>
        public static void Init()
        {
            // Ensure all resource providers are loaded
            if (resourceProviders == null)
                resourceProviders = new List<IResourceProvider>();
            resourceProviders.Clear();

            foreach (string gameDirectory in settings.GamePaths)
            {
                SearchGameInfo(gameDirectory);
            }

            // Add every bsp file's pak lump
            if (settings.readBSPFiles)
            {
                MemoryStream stream = new(1 << 22);
                for (int i = resourceProviders.Count - 1; i >= 0; i--)
                {
                    IResourceProvider provider = resourceProviders[i];
                    foreach (string bspFile in provider.GetFiles().Where<string>(e => Path.GetExtension(e) == ".bsp"))
                    {
                        stream.Position = 0;
                        provider.OpenFile(bspFile, stream);
                        BSPProvider bspProvider = new BSPProvider(stream);
                        resourceProviders.Add(bspProvider);
                    }
                }
                stream.Dispose();
            }

            //Debug.Log($"Found {resourceProviders.Count} resource providers");
        }
        /// <summary>
        /// Creates resource providers through a game's gameinfo.txt file
        /// </summary>
        /// <param name="gameInfoPath"></param>
        static void SearchGameInfo(string gameRootPath)
        {
            string[] splitters = new string[] {
                "\t",
                " "
            };

            if (File.Exists($"{gameRootPath}/gameinfo.txt"))
            {
                string gameInfoText = File.ReadAllText($"{gameRootPath}/gameinfo.txt");
                KeyValues keyValues = KeyValues.Parse(gameInfoText);
                //Debug.Log(gameInfoText);
                KeyValues.Entry values = keyValues["gameinfo"].First();

                string parentPath = System.IO.Directory.GetParent(gameRootPath).FullName;

                foreach (KeyValuePair<string, KeyValues.Entry> pair in values["filesystem"].First()["searchpaths"].First())
                {
                    /*
                     * For each path, check whether it is referencing a VPK file or a directory
                     * If a directory, make sure the DirProvider's root is correctly set
                     */

                    // Convert to system directory
                    string path = pair.Value.Value.Replace("|all_source_engine_paths|", "").Replace("|gameinfo_path|.", "").Trim('*');
                    string extension = System.IO.Path.GetExtension(path);

                    path = $"{parentPath}/{path}";

                    if (string.IsNullOrEmpty(extension))
                    {
                        // Directory
                        if (System.IO.Directory.Exists(path))
                        {
                            resourceProviders.Add(new DirProvider(path));
                            //Debug.Log($"Added directory: {path}");
                        }
                    }
                    else if (extension == ".vpk")
                    {
                        // VPK files are referenced without "_dir" despite their real paths including it
                        // the real VPK file will have _dir on the end

                        path = path.Replace(".vpk", "_dir.vpk");
                        if (System.IO.File.Exists(path))
                        {
                            resourceProviders.Add(new VPKProvider(path));
                            //Debug.Log($"Added VPK: {path}");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Attempts to find a resource provider which contains the given source asset location
        /// </summary>
        /// <param name="location"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static bool TryFindResourceProvider(Location location, out IResourceProvider provider)
        {
            foreach (IResourceProvider otherProvider in resourceProviders)
            {
                if (otherProvider.ContainsFile(location.SourcePath))
                {
                    provider = otherProvider;
                    return true;
                }
            }

            provider = null;
            return false;
        }
        public static bool TryFindResourceProviderOpenFile(Location location, out IResourceProvider provider, out Stream stream)
        {
            foreach (IResourceProvider otherProvider in resourceProviders)
            {
                if (otherProvider.TryGetFile(location.SourcePath, out stream))
                {
                    provider = otherProvider;
                    return true;
                }
            }

            provider = null;
            stream = null;
            return false;
        }
        //public static bool TryImportAssetInline<T>(Location location, out T resource) where T : UnityEngine.Object
        //{
        //    resource = null;
        //    if (TryFindResourceProvider(location, out IResourceProvider provider) == false)
        //    {
        //        return false;
        //    }
        //    Converter converter = Converter.FromLocation(location, provider, );
        //}
        public static bool TryImportAsset<T>(Location location, out T resource, ImportMode mode = ImportMode.ReadFromAssetDataBaseDontImport, bool reloadAssets = false) where T : UnityEngine.Object
        {
            resource = null;

            if (mode == ImportMode.ReadFromAssetDataBaseDontImport || mode == ImportMode.ReadFromAssetDataBaseOrImport)  // If our import mode will try and load from the Asset database first
            {
                resource = AssetDatabase.LoadAssetAtPath<T>(location.AssetPath);

                if (resource != null)
                    return true;
                else if (mode == ImportMode.ReadFromAssetDataBaseDontImport)  // If the resource does not exist and the import mode will not try and import from Source, return
                    return false;
            }

            // Asset does not exist, load from Source files

            // Get data stream
            if (TryFindResourceProvider(location, out IResourceProvider provider) == false)
                return false;
            Stream stream = provider.OpenFile(location.SourcePath);

            // Create converter
            Converter converter = Converter.FromLocation(location, provider, stream);

            stream.Close();

            // Build dependency list
            List<(Location, int)> dependencies = new() { (location, 0) };  // import self into this
            List<(Location, int)> searchList = new()
            {
                (location, 0)
            };

            while (searchList.Count > 0)
            {
                (Location, int) currentLocation = searchList[0];
                searchList.RemoveAt(0);

                if (TryFindResourceProvider(currentLocation.Item1, out IResourceProvider providerDependency))
                {
                    Stream streamDependency = providerDependency.OpenFile(currentLocation.Item1.SourcePath);
                    Converter converterDependency = Converter.FromLocation(currentLocation.Item1, providerDependency, streamDependency);
                    streamDependency.Close();

                    if (converterDependency != null)
                    {
                        foreach (string sourceDependency in converterDependency.GetSourceAssetDependencies())
                        {
                            dependencies.Add((new Location(sourceDependency, Location.Type.Source), currentLocation.Item2 + 1));
                            searchList.Add((new Location(sourceDependency, Location.Type.Source), currentLocation.Item2 + 1));
                        }
                    }
                }
            }

            // Begin importing files based on dependency order / importance
            dependencies.Sort(((Location, int) a, (Location, int) b) => {
                return b.Item2.CompareTo(a.Item2);
            });

            List<Location> importedAssets = new();

            foreach ((Location, int) sourceDependency in dependencies)
            {
                if (TryImportFile(sourceDependency.Item1))
                    importedAssets.Add(sourceDependency.Item1);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);

            foreach (Location importedLocation in importedAssets)
            {
                // attempt to get the asset importer and dirty/save it
                AssetImporter importer = AssetImporter.GetAtPath(importedLocation.AssetPath);
                UnityEngine.Object importedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(importedLocation.AssetPath);
                if (importer != null && importedAsset != null)
                {
                    EditorUtility.SetDirty(importedAsset);
                    UnityEditor.EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    AssetDatabase.SaveAssetIfDirty(importedAsset);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);

            resource = AssetDatabase.LoadAssetAtPath<T>(location.AssetPath);
            return resource != null;
        }
        /// <summary>
        /// Attemps to import a single file from Source into the Asset database
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static bool TryImportFile(Location location, IResourceProvider provider = null)
        {
            // Load file
            // Create converter and load dependencies

            if ((provider != null && provider.ContainsFile(location.SourcePath) == false) || provider == null)  // Provided ResourceProvider doesnt have asset
                if (TryFindResourceProvider(location, out provider) == false)
                    return false;

            Stream stream = provider.OpenFile(location.SourcePath);

            if (stream == null)
                return false;

            // Copy to asset database
            // Ensure directory exists
            string absolutePath = location.AbsolutePath.Replace('\\', '/');
            int lastSlash = absolutePath.LastIndexOf('/') + 1;
            System.IO.Directory.CreateDirectory(absolutePath.Remove(lastSlash, absolutePath.Length - lastSlash));
            using FileStream file = new FileStream(location.AbsolutePath, System.IO.FileMode.Create);
            stream.CopyTo(file);

            file?.Close();
            stream?.Close();

            return true;
        }
        public static bool TryResolveMaterialPath(string materialPath, out Location resolvedMaterial)
        {
            // materials/maps/beta house map/building_template/building_template002b_2219_-232_207.vmt      <- CONVERT FROM THIS
            // materials/building_template/building_template002b.vmt                                        <- TO THIS

            // Remove the three last sets of digits
            // Begin searching for a vmt using the existing path, removing a folder with each unsuccessful attempt
            resolvedMaterial = default;
            int digitsIndex = materialPath.Length - 1;
            int digitsCounter = 0;
            for (int c = materialPath.Length - 1; c >= 0; c--)
            {
                char character = materialPath[c];
                if (character == '_')
                {
                    digitsCounter++;
                    digitsIndex = c;
                }

                if (digitsCounter == 3)
                {
                    break;
                }
            }

            if (digitsCounter != 3)
                return false;

            string modifiedResourcePath = materialPath.Remove(digitsIndex, materialPath.Length - digitsIndex - 4);  // 4 is from the extension (.vmt)

            int iteration = 0;
            while (true && iteration < 20)
            {
                iteration++;

                if (TryFindResourceProvider(new Location(modifiedResourcePath, Location.Type.Source), out IResourceProvider provider))
                {
                    resolvedMaterial = new Location(modifiedResourcePath, Location.Type.Source, provider);
                    return true;
                }
                else
                {
                    string[] folders = modifiedResourcePath.Split('/');
                    if (folders.Length <= 1)
                        return false;

                    // Remove the first folder that isn't the materials root

                    modifiedResourcePath = string.Empty;
                    for (int i = 0; i < folders.Length; i++)
                    {
                        if (i == 1)
                            continue;

                        modifiedResourcePath += folders[i] + (i == folders.Length - 1 ? "" : "/");
                    }
                }
            }

            return false;
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
        public static bool CreateUnityObject(Location location, List<Location> dependencies, out UnityEngine.Object unityObject)
        {
            if (objectCache.TryGetValue(location.CopyNoResourceLocation(), out unityObject))
            {
                return true;
            }

            // go in reverse order
            unityObject = null;

            for (int i = dependencies.Count - 1; i >= 0; i--)
            {
                // check if object already exists in cache
                // if not, create it
                Location dependency = dependencies[i];
                Location dependencyNoResourceLocation = dependency.CopyNoResourceLocation();
                if (objectCache.TryGetValue(dependencyNoResourceLocation, out unityObject))  // Asset exists in cache
                {
                    //Debug.Log("exists in cache");
                    continue;
                }

                // Find a resource provider that has the data
                if (((dependencies[0].ResourceProvider != null && dependencies[0].ResourceProvider.TryGetFile(dependency.SourcePath, out Stream stream)) ||  // Does parent asset have the file
                    TryFindResourceProviderOpenFile(location, out _, out stream)) == false)  // Does any resource provider have the file
                {
                    // Neither methods gave the file
                    //Debug.Log("does not exist");
                    continue;
                }

                // create object and store in cache
                Converter converter = Converter.FromLocation(location, null, stream);
                unityObject = converter.CreateAsset(default, true);
                if (unityObject != null)
                {
                    objectCache[dependencyNoResourceLocation] = unityObject;
                }
                stream.Close();
                //Debug.Log("exists -- created");
            }

            return unityObject != null;
        }
        //public static T GetSourceAsset<T>(Location location) where T : UnityEngine.Object
        //{
        //    // get converter

        //}
        //public static T GetConverter<T>(Location location) where T : Converter
        //{
        //    List<Location> dependencies = new(){ location };

        //    ISourceAsset asset = null;
        //    asset.GetDependencies()
        //}
    }
}