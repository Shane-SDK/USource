using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace USource
{
    [CustomEditor(typeof(Settings))]
    public class SettingsInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Settings settings = target as Settings;

            base.OnInspectorGUI();

            EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
            EditorGUILayout.LabelField("Game Paths ( Folder must contain gameinfo.txt )");
            if (GUILayout.Button("Add Path"))
            {
                string directory = settings.GamePaths.Count > 0 ? settings.GamePaths[^1] : Application.persistentDataPath;
                //string path = EditorUtility.OpenFilePanel("Select a directory containing gameinfo.txt", directory, "");
                string path = EditorUtility.OpenFolderPanel("Select a directory containing gameinfo.txt", directory, "");

                if (System.IO.Path.HasExtension(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }

                if (System.IO.Directory.Exists(path))
                {
                    settings.GamePaths.Add(path);
                    EditorUtility.SetDirty(settings);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();
            for (int i = 0; i < settings.GamePaths.Count; i++)
            {
                bool escape = false;
                string path = settings.GamePaths[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(path);
                if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                {
                    escape = true;
                    settings.GamePaths.RemoveAt(i);
                    EditorUtility.SetDirty(settings);
                }
                EditorGUILayout.EndHorizontal();

                if (escape)
                {
                    break;
                }
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}