//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using USource.AssetImporters;
//using UnityEngine.UIElements;
//using UnityEditor.UIElements;
//using UnityEditor.SceneManagement;

//namespace USource
//{
//    [CustomEditor(typeof(MdlImporter))]
//    public class MdlImporterEditor : Editor
//    {
//        public override void OnInspectorGUI()
//        {
//            MdlImporter importer = (MdlImporter)target;
//            EditorGUI.BeginChangeCheck();
//            EditorGUILayout.PropertyField(serializedObject.FindProperty("importGeometry"));
//            EditorGUILayout.PropertyField(serializedObject.FindProperty("importPhysics"));
//            GUIStyle style = new GUIStyle();
//            style.fontStyle = FontStyle.Bold;
//            EditorGUILayout.LabelField("Animation support is underdeveloped and buggy", style);
//            EditorGUILayout.PropertyField(serializedObject.FindProperty("importAnimations"));
//            if (EditorGUI.EndChangeCheck())
//            {
//                EditorUtility.SetDirty(importer);
//                serializedObject.ApplyModifiedProperties();
//            }
//        }
//    }
//}
