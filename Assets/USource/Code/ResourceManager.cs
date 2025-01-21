using System.IO;
using System.Linq;
using System.Collections.Generic;
using USource.Converters;
using USource.SourceAsset;
using USource.Providers;
using System.Diagnostics;

namespace USource
{
    public class ResourceManager
    {
        public List<IResourceProvider> ResourceProviders { get { return resourceProviders; } }
        List<IResourceProvider> resourceProviders = new List<IResourceProvider>();
        public IReadOnlyDictionary<Location, UnityEngine.Object> ObjectCache => objectCache;
        Dictionary<Location, UnityEngine.Object> objectCache = new();
        //Process zipProcess;
        public ResourceManager()
        {
            Refresh();

        }
        void CreateResourceProviders()
        {
            void SearchGameInfo(string gameRootPath)
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

            // Ensure all resource providers are loaded
            if (resourceProviders == null)
                resourceProviders = new List<IResourceProvider>();
            resourceProviders.Clear();

            foreach (string gameDirectory in USource.settings.GamePaths)
            {
                SearchGameInfo(gameDirectory);
            }

            // Add every bsp file's pak lump
            if (USource.settings.readBSPFiles)
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
        public void Refresh()
        {
            objectCache = new();
            CreateResourceProviders();
        }
        public bool TryFindResourceProvider(Location location, out IResourceProvider provider)
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
        public bool TryFindResourceProviderOpenFile(Location location, out IResourceProvider provider, out Stream stream)
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
        public bool CreateUnityObject(Location location, out UnityEngine.Object unityObject)
        {
            if (GetUnityObjectFromCache(location, out unityObject, true))
                return true;

            // Get list of dependencies
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            System.IO.Stream stream = location.ResourceProvider[location];
            DependencyTree dependencies = new(location);
            sourceAsset.GetDependencies(stream, dependencies, true, ImportMode.CreateAndCache);
            List<Location> depList = dependencies.RecursiveChildren.ToList();
            // go in reverse order
            // ensure every dependency exists before attempting to create most-dependent object
            for (int i = depList.Count - 1; i >= 0; i--)
            {
                // check if object already exists in cache
                Location dependency = depList[i];
                if (dependency.GetAssetType() == AssetType.None)  // Not a supported format
                    continue;

                if (GetUnityObjectFromCache(dependency, out unityObject, true))  // Asset exists in cache
                    continue;

                // Asset hasn't been made at this point, create the asset and cache it

                // Find a resource provider that has the data
                if (((depList[0].ResourceProvider != null && depList[0].ResourceProvider.TryGetFile(dependency.SourcePath, out stream)) ||  // Does parent asset have the file
                    TryFindResourceProviderOpenFile(dependency, out _, out stream)) == false)  // Does any resource provider have the file
                    continue;  // Neither methods gave the file


                // create object and store in cache
                Converter converter = Converter.FromLocation(dependency, stream);
                unityObject = converter.CreateAsset( new ImportContext(ImportMode.CreateAndCache) );
                if (unityObject != null)
                    Cache(dependency, unityObject);

                stream.Close();
            }

//#if UNITY_EDITOR
//            UnityEditor.AssetDatabase.Refresh();
//#endif

            return unityObject != null;
        }
#if UNITY_EDITOR
        public void ImportSourceAssetToAssetDatabase(Location location, bool reimportExistingDependencies = true)
        {
            /*
             * Get dependencies
             * Copy file from resource provider to asset database
             */
            UnityEngine.Profiling.Profiler.BeginSample("ImportSourceAssetToDatabase");
            UnityEditor.AssetDatabase.StartAssetEditing();

            if (!location.ResourceProvider.TryGetFile(location.SourcePath, out Stream stream) && !TryFindResourceProviderOpenFile(location, out _, out stream))
            {  // Cannot locate file
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            UnityEditor.EditorUtility.DisplayProgressBar($"Importing {location.SourcePath}", "[1/2] Finding Dependencies", 0.0f);
            DependencyTree dependencies = new(location);
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            sourceAsset.GetDependencies(stream, dependencies, true, ImportMode.CreateAndCache);
            List<Location> depList = dependencies.RecursiveChildren.ToList();
            stream.Close();

            for (int i = depList.Count - 1; i >= 0; i--)
            {
                Location dependency = depList[i];

                int reverseIndex = (depList.Count - 1 - i);
                UnityEditor.EditorUtility.DisplayProgressBar($"Importing {location.SourcePath}", $"[2/2] Importing {dependency.SourcePath} ({reverseIndex} / {depList.Count - 1})", (float)(reverseIndex) / (depList.Count - 1));
                if (!reimportExistingDependencies && (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dependency.AssetPath) != null))  // If dependency has already been imported
                    continue;

                Stream assetStream = null;
                if (!(depList[i].ResourceProvider != null && depList[i].ResourceProvider.TryGetFile(dependency.SourcePath, out assetStream)) &&  // Couldn't find data in immediate location
                    !(TryFindResourceProviderOpenFile(dependency, out _, out assetStream)))  // Data isn't in providers
                    continue;  // Data doesn't exist

                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(dependency.AssetPath));
                using (var fileStream = File.Create(dependency.AssetPath, 16, FileOptions.WriteThrough))
                {
                    assetStream.CopyTo(fileStream);
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.AssetDatabase.StopAssetEditing();
            UnityEngine.Profiling.Profiler.EndSample();
        }
#endif
        void Cache(Location location, UnityEngine.Object obj)
        {
            objectCache[location] = obj;
            // todo -- prioritize certain resource locations???
            objectCache[location.CopyNoResourceLocation()] = obj;
        }
        public bool GetUnityObjectFromCache(Location location, out UnityEngine.Object obj, bool useResourceProvider = false)
        {
            if (useResourceProvider && objectCache.TryGetValue(location, out obj))
                return true;

            return objectCache.TryGetValue(location.CopyNoResourceLocation(), out obj);
        }
        public bool GetUnityObjectFromCache<T>(Location location, out T castObject, bool useResourceProvider = false) where T : UnityEngine.Object 
        {
            if (GetUnityObjectFromCache(location, out UnityEngine.Object obj, useResourceProvider) && obj is T)
            {
                castObject = (T)obj;
                return true;
            }

            castObject = null;
            return false;
        }
        public bool GetUnityObject<T>(Location location, out T unityObject, ImportMode importMode, bool useResourceProvider = false) where T : UnityEngine.Object
        {
            unityObject = null;
            if (importMode == ImportMode.CreateAndCache && GetUnityObjectFromCache<T>(location, out unityObject, useResourceProvider))
                return true;
#if UNITY_EDITOR
            else if (importMode == ImportMode.AssetDatabase)
            {
                unityObject = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(location.AssetPath);
                return unityObject != null;
            }
#endif

            return false;
        }
        public bool GetStream(Location location, out Stream stream, ImportMode mode = ImportMode.CreateAndCache)
        {
            if (mode == ImportMode.CreateAndCache && TryFindResourceProviderOpenFile(location, out _, out stream))
                return true;
#if UNITY_EDITOR
            else if (mode == ImportMode.AssetDatabase && System.IO.File.Exists(location.AbsolutePath))
            {
                stream = File.OpenRead(location.AbsolutePath);
                return true;
            }
#endif

            stream = null;
            return false;
        } 
        //public bool TryResolveMaterialPath(string materialPath, out Location resolvedMaterial)
        //{
        //    // materials/maps/beta house map/building_template/building_template002b_2219_-232_207.vmt      <- CONVERT FROM THIS
        //    // materials/building_template/building_template002b.vmt                                        <- TO THIS

        //    // Remove the three last sets of digits
        //    // Begin searching for a vmt using the existing path, removing a folder with each unsuccessful attempt
        //    resolvedMaterial = default;
        //    int digitsIndex = materialPath.Length - 1;
        //    int digitsCounter = 0;
        //    for (int c = materialPath.Length - 1; c >= 0; c--)
        //    {
        //        char character = materialPath[c];
        //        if (character == '_')
        //        {
        //            digitsCounter++;
        //            digitsIndex = c;
        //        }

        //        if (digitsCounter == 3)
        //        {
        //            break;
        //        }
        //    }

        //    if (digitsCounter != 3)
        //        return false;

        //    string modifiedResourcePath = materialPath.Remove(digitsIndex, materialPath.Length - digitsIndex - 4);  // 4 is from the extension (.vmt)

        //    int iteration = 0;
        //    while (true && iteration < 20)
        //    {
        //        iteration++;

        //        if (TryFindResourceProvider(new Location(modifiedResourcePath, Location.Type.Source), out IResourceProvider provider))
        //        {
        //            resolvedMaterial = new Location(modifiedResourcePath, Location.Type.Source, provider);
        //            return true;
        //        }
        //        else
        //        {
        //            string[] folders = modifiedResourcePath.Split('/');
        //            if (folders.Length <= 1)
        //                return false;

        //            // Remove the first folder that isn't the materials root

        //            modifiedResourcePath = string.Empty;
        //            for (int i = 0; i < folders.Length; i++)
        //            {
        //                if (i == 1)
        //                    continue;

        //                modifiedResourcePath += folders[i] + (i == folders.Length - 1 ? "" : "/");
        //            }
        //        }
        //    }

        //    return false;
        //}
    }
    public enum LoadOption
    {
        Cache,
        AssetDatabase,
        UserFiles
    }
}