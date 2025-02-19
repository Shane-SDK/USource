using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using USource.AssetImporters;
using USource.Converters;
using USource.Formats.BSP;
using System.IO;
using UnityEditor.AssetImporters;

namespace USource
{
    [CustomEditor(typeof(BspImporter))]
    public class BspImporterInspector : ScriptedImporterEditor
    {
        MemoryStream stream;
        UReader reader;
        Formats.BSP.Header header;
        public override void OnEnable()
        {
            base.OnEnable();

            using (FileStream file = File.OpenRead((target as BspImporter).assetPath))
            {
                stream = new MemoryStream();
                file.CopyTo(stream);
                reader = new UReader(stream);
                reader.BaseStream.Position = 0;
                header.ReadToObject(reader, 0);
            }
        }
        public override void OnDisable()
        {
            base.OnDisable();

            reader?.Close();
            stream?.Close();
        }
        public override void OnInspectorGUI()
        {
            BspImporter importer = target as BspImporter;

            base.OnInspectorGUI();
            GUILayout.Space(4);
            GUILayout.Label($"BSP Version: {header.version}");
            if (GUILayout.Button("Import and Apply Skybox"))
            {
                BSP bsp = new BSP(stream);
                if (bsp.entities.Count > 0 && bsp.entities[0].values.TryGetValue("skyname", out string skyName))
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>($"{USource.AssetsPath}UnitySkyboxes/{skyName}.asset");
                    if (material == null && SkyboxUtility.CreateUnitySkyMaterial(skyName, ImportMode.AssetDatabase, out Material unitySkyMaterial))
                    {
                        SkyboxUtility.SaveUnitySkyMaterial(skyName, unitySkyMaterial);
                        RenderSettings.skybox = unitySkyMaterial;
                    }
                }
            }
        }
    }
}