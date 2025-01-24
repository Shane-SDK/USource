using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace USource
{
    [CreateAssetMenu(fileName="Settings", menuName ="USource/Settings")]
    public class Settings : ScriptableObject
    {
        public List<string> GamePaths => gamePaths;
        public float sourceToUnityScale = 0.025f;
#if UNITY_EDITOR
        [HideInInspector]
        public UnityEditor.Presets.Preset textureImporterPreset;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector]
#endif
        List<string> gamePaths;
        [Header("WARNING: SLOW!")]
        public bool readBSPFiles = false;
        private void OnAwake()
        {
            if (gamePaths == null)
                gamePaths = new List<string>();
        }
    }

}