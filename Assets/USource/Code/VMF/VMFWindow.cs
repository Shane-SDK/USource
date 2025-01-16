using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace USource.VMF
{
    public class VMFEditor : UnityEditor.EditorWindow
    {
        [MenuItem("USource/VMF Importer")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            VMFEditor window = (VMFEditor)EditorWindow.GetWindow(typeof(VMFEditor));
            window.Show();
        }
        private void CreateGUI()
        {
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset VMF", new[] { "Assets/USource" });
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0])).CloneTree(rootVisualElement);

            rootVisualElement.Q<Button>("import").clicked += VMFEditor_clicked;
        }

        private void VMFEditor_clicked()
        {
            string path = EditorUtility.OpenFilePanel("Select a VMF", USource.settings.GamePaths[0] + "/maps", "vmf");

            if (string.IsNullOrEmpty(path) || System.IO.File.Exists(path) == false)
                return;

            //VMF.CreateFromVMF(path);
        }
    }
}