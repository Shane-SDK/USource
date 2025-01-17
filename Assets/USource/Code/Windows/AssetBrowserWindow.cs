using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using USource.SourceAsset;

namespace USource.Windows
{
    public class AssetBrowserWindow : UnityEditor.EditorWindow
    {
        static readonly string[] extensions = new string[]
        {
            ".vmt",
            ".vtf",
            ".mdl",
            ".vmf",
            //".wav",
            //".mp3"
        };
        static readonly HashSet<string> extensionSet = new();
        [MenuItem("USource/Asset Browser")]
        static void Open()
        {
            extensionSet.Clear();
            foreach (string s in extensions)
                extensionSet.Add(s);

            GetWindow<AssetBrowserWindow>("Asset Browser");
        }
        string Filter
        {
            get
            {
                return rootVisualElement.Q<TextField>("filter").value.ToLower();
            }
        }
        List<Location> entries = new();
        List<int> entryIndices = new();
        int currentInstanceId;
        private void CreateGUI()
        {
            USource.Init();

            if (TryLoadAsset("t:VisualTreeAsset AssetBrowser", new[] { "Assets/USource" }, out VisualTreeAsset rootAsset) == false)
            {
                Debug.LogError($"Failed to find Asset Browser VisualTreeAsset asset in USource directory", this);
                return;
            }

            rootAsset.CloneTree(rootVisualElement);

            rootVisualElement.Q<TextField>("filter").RegisterCallback<ChangeEvent<string>>((e) =>
            {
                ApplyFilter();
            });

            // get entry asset
            if (TryLoadAsset("t:VisualTreeAsset Entry", new[] { "Assets/USource" }, out VisualTreeAsset entryAsset) == false)
                return;

            ListView rootView = rootVisualElement.Q<ListView>();
            rootView.itemsSource = entryIndices;
            rootView.makeItem = () =>
            {
                VisualElement element = entryAsset.Instantiate();
                element.style.height = new StyleLength(StyleKeyword.Auto);
                element.style.flexShrink = 1;
                return element;
            };
            rootView.bindItem = (VisualElement element, int index) =>
            {
                Location entry = entries[entryIndices[index]];
                element.Q<Label>("path").text = $"{entry.SourcePath}";
                element.Q("provider").Q<Label>().text = $"{entry.ResourceProvider.GetName()}";
                //if (index % 2 == 0)
                //    element.RemoveFromClassList("entry-2n");
                //else
                //    element.AddToClassList("entry-2n");
            };
            rootView.selectedIndicesChanged += (indices) =>
            {
                Location location = entries[entryIndices[indices.First()]];
                if (USource.ResourceManager.CreateUnityObject(location, out UnityEngine.Object obj))
                {
                    currentInstanceId = obj.GetInstanceID();

                    if (obj is GameObject)
                        DestroyImmediate(obj);
                }
            };
            rootVisualElement.Q<Button>("refresh").clicked += () =>
            {
                USource.Init();
                RefreshListView();
            };

            rootVisualElement.Q<Button>("import").clicked += () =>
            {
                foreach (int index in rootView.selectedIndices)
                {
                    Location location = entries[entryIndices[index]];
                    USource.ResourceManager.ImportSourceAssetToAssetDatabase(location);
                }

                AssetDatabase.RefreshSettings();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            };

            RebuildEntries();
            ApplyFilter();
            RefreshListView();
        }
        private void OnGUI()
        {
            UnityEngine.Object unityObject = EditorUtility.InstanceIDToObject(currentInstanceId);
            Texture2D tex = AssetPreview.GetAssetPreview(unityObject);
            rootVisualElement.Q("preview").style.backgroundImage = new StyleBackground(tex);
        }
        void RebuildEntries()
        {
            entries.Clear();
            entryIndices.Clear();

            foreach (IResourceProvider provider in USource.ResourceManager.ResourceProviders)
            {
                foreach (string path in provider.GetFiles())
                {
                    Location location = new Location(path, Location.Type.Source, provider);
                    string extension = System.IO.Path.GetExtension(path);

                    if (extensionSet.Contains(extension) == false)
                        continue;

                    entries.Add(location);
                }
            }

            rootVisualElement.Q<ListView>().ClearSelection();
        }
        void ApplyFilter()
        {
            entryIndices.Clear();

            string[] filters = Filter.Split(' ');

            for (int i = 0; i < entries.Count; i++)
            {
                Location location = entries[i];
                string newLocation = location.SourcePath.ToLower();
                if (filters.All(e => newLocation.Contains(e)))
                    entryIndices.Add(i);
            }

            RefreshListView();
        }
        void RefreshListView()
        {
            ListView rootView = rootVisualElement.Q<ListView>();
            //rootView.Rebuild();
            rootView.RefreshItems();

            rootVisualElement.Q("header").Q<Label>().text = $"{entryIndices.Count}/{entries.Count} assets found";
        }
        bool TryLoadAsset(string name, string[] folders, out VisualTreeAsset asset)
        {
            asset = null;

            string[] guids = AssetDatabase.FindAssets(name, folders);
            if (guids.Length == 0)
            {
                Debug.LogError($"Failed to find VisualTreeAsset asset in USource directory", this);
                return false;
            }

            asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return asset != null;
        }
    }
}
