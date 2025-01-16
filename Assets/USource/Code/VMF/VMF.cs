//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//#if UNITY_EDITOR
//using RealtimeCSG;
//using RealtimeCSG.Components;
//using VMFParser;
//#endif
//using System.Text.RegularExpressions;
//using System.Linq;
//using USource.AssetImporters;
//using UnityEditor;
//using USource.MathLib;
//using USource.Converters;

//namespace USource.VMF
//{
//    public enum Layer
//    {
//        Solid,
//        Transparent,
//        Clipping,
//        Water,
//        Skip
//    }
//    public class VMF
//    {
//        const string rMatchNumbers = @"[-+]?([0-9]*\.[0-9]+|[0-9]+)";
//        const string rMatchParentheses = @"\(([^()]+)\)";
//        public static readonly string[] noRenderMaterials = new string[]
//        {
//            "materials/tools/toolsnodraw"
//        };
//        public static readonly string[] noCreateMaterials = new string[]
//        {
//            "materials/tools/toolsskybox",
//            "materials/tools/toolsskip",
//            "materials/tools/toolsplayerclip",
//            "materials/tools/toolshint",
//            "materials/tools/toolsclip"
//        };
//#if UNITY_EDITOR
//        // Load VMF
//        public static void CreateFromVMF(string path)
//        {
//            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            
//            // Ensure uSource is initialized
//            USource.Init();

//            GameObject vmfGO = GameObject.Find("VMF");

//            //UnityEditor.AssetDatabase.StartAssetEditing();
//            {
//                if (vmfGO != null)
//                    GameObject.DestroyImmediate(vmfGO);
//            }

//            vmfGO = new GameObject("VMF");

//            string[] text = System.IO.File.ReadAllLines(path);
//            VMFParser.VMF vmf = new VMFParser.VMF(text);

//            Regex regexMatchNumbers = new Regex(rMatchNumbers, RegexOptions.Compiled);
//            Regex regexMatchParentheses = new Regex(rMatchParentheses, RegexOptions.Compiled);

//            // For each model (set of CSG brushes)
//            // - Solid, transparent, and clipping models

//            GameObject CreateModels(VBlock block)
//            {
//                GameObject root = new GameObject();
//                root.isStatic = true;
//                Dictionary<Layer, CSGModel> models = new Dictionary<Layer, CSGModel>();

//                foreach (IVNode node in block.Body)
//                {
//                    if (node is VBlock childBlock)
//                    {
//                        CSGBrush brush = Create(childBlock, out Layer layer);

//                        if (brush == null)
//                            continue;

//                        // Ensure CSG model actually exists
//                        if (models.TryGetValue(layer, out CSGModel model) == false)
//                        {
//                            model = new GameObject(layer.ToString()).AddComponent<CSGModel>();
//                            model.Settings |= ModelSettingsFlags.TwoSidedShadows;
//                            model.transform.parent = root.transform;
//                            model.ReceiveGI = ReceiveGI.Lightmaps;
//                            model.gameObject.isStatic = true;

//                            models[layer] = model;
//                        }

//                        brush.transform.parent = model.transform;
//                    }
//                }

//                return root;
//            }

//            CSGBrush Create(VBlock block, out Layer layer)
//            {
//                layer = Layer.Solid;
//                UnityEngine.Profiling.Profiler.BeginSample("CreateBrush");
//                bool disable = false;

//                // Create CSG Brush
//                int planeCount = block.Body.Count((IVNode node) => { return node.Name == "side"; });  // Count how many planes will be in the brush

//                if (planeCount < 4)
//                    return null;

//                Plane[] planes = new Plane[planeCount];
//                UnityEngine.Material[] materials = new UnityEngine.Material[planeCount];
//                Matrix4x4[] textureMatrices = new Matrix4x4[planeCount];
//                float[] rotations = new float[planeCount];
//                RealtimeCSG.Legacy.TexGenFlags[] flags = new RealtimeCSG.Legacy.TexGenFlags[planeCount];
//                int i = 0;

//                // "uaxis" "[0 -1 0 -160] 0.25"
//                // "vaxis" "[0 0 -1 288] 0.25"

//                foreach (VBlock side in block.Body.Where((IVNode node) => { return node.Name == "side"; }))
//                {
//                    // Get plane
//                    side.TryGetValue("plane", out string planeString);
//                    planes[i] = GetPlaneFromString(planeString);
//                    // Get material
//                    if (side.TryGetValue("material", out string materialPath) == false) { i++; continue; }  // Material does not exist

//                    Location location = new Location($"materials/{materialPath}.vmt", Location.Type.Source);
//                    if (ResourceManager.TryResolveMaterialPath(location.SourcePath, out Location resolvedMaterial))
//                    {
//                        location = resolvedMaterial;
//                    }

//                    ResourceManager.TryImportAsset<UnityEngine.Material>(location, out UnityEngine.Material resourceMaterial, ResourceManager.ImportMode.ReadFromAssetDataBaseOrImport, true);

//                    if (resourceMaterial == null)
//                    {
//                        Debug.Log($"Missing material: {location.SourcePath}");
//                        resourceMaterial = Resources.Load<UnityEngine.Material>("Error");
//                    }

//                    if (resourceMaterial != null)
//                    {
//                        materials[i] = resourceMaterial;
                     
//                        VmtImporter importer = AssetImporter.GetAtPath(location.AssetPath) as VmtImporter;
//                        if (importer != null)
//                        {
//                            if (importer.flags.HasFlag(MaterialFlags.Invisible))
//                                flags[i] |= RealtimeCSG.Legacy.TexGenFlags.NoRender;
//                            if (importer.flags.HasFlag(MaterialFlags.NoShadows))
//                                flags[i] |= RealtimeCSG.Legacy.TexGenFlags.NoCastShadows;
//                            if (importer.flags.HasFlag(MaterialFlags.NonSolid))
//                                flags[i] |= RealtimeCSG.Legacy.TexGenFlags.NoCollision;

//                            if (importer.flags == MaterialFlags.NonSolid)
//                                layer = Layer.Transparent;

//                            if (importer.flags.HasFlag(MaterialFlags.Invisible) && importer.flags.HasFlag(MaterialFlags.NonSolid) == false)
//                            {
//                                layer = Layer.Clipping;
//                            }

//                            //if (importer.flags.HasFlag(AssetInfo.MaterialFlags.Water))
//                            //    layer = Layer.Water;
//                        }

//                        Vector2 uvScale = Vector2.one;
//                        Vector2 uvTranslation = Vector2.zero;
//                        Vector3 uWorldDirection = default;
//                        Vector3 vWorldDirection = default;

//                        for (int u = 0; u < 2; u++)
//                        {
//                            string axisKey = u == 0 ? "uaxis" : "vaxis";
//                            side.TryGetValue(axisKey, out string uvData);
//                            GetUVData(uvData, out Vector3 direction, out float translation, out float scale);

//                            // translation : how many hammer units to adjust uv
//                            // scale : uv scale
//                            // uvRotation : WORLD SPACE uv direction

//                            // Source translation: pixels to adjust texture by
//                            // Source scaling: size of the texture - scaling of 1 means one pixel is one hammer unit in size

//                            // Unity translation: normalized amount of texture to offset by
//                            // Unity scaling: size of the texture

//                            // Scale texture based on image size
//                            int imageSize = 512;
//                            if (resourceMaterial != null && resourceMaterial.HasProperty("_MainTex") && resourceMaterial.mainTexture != null)
//                                imageSize = u == 0 ? resourceMaterial.mainTexture.width : resourceMaterial.mainTexture.height;

//                            float sizeFactor = imageSize / 512.0f;

//                            uvScale[u] = Converter.uvScaleFactor / (scale * 16) / sizeFactor;
//                            uvTranslation[u] = translation / imageSize;

//                            if (u == 0)
//                                uWorldDirection = Converter.AxisConvertSource(direction);
//                            else
//                                vWorldDirection = Converter.AxisConvertSource(direction);
//                        }

//                        // Convert uv directions from world to local space
//                        Vector3 uvWorldCross = Vector3.Cross(vWorldDirection, uWorldDirection);
//                        //Quaternion worldToLocalRotation = Quaternion.FromToRotation(uvWorldCross, Vector3.forward);
//                        Quaternion worldToLocalRotation = Quaternion.Inverse(Quaternion.LookRotation(uvWorldCross));
//                        Vector3 uLocalDirection = worldToLocalRotation * uWorldDirection;
//                        Vector3 vLocalDirection = worldToLocalRotation * vWorldDirection;

//                        uvScale.x *= -Mathf.Sign(uLocalDirection.x);
//                        uvScale.y *= -Mathf.Sign(vLocalDirection.y);

//                        uvTranslation.x *= Mathf.Sign(uLocalDirection.x);
//                        uvTranslation.y *= Mathf.Sign(vLocalDirection.y);

//                        rotations[i] = Vector3.SignedAngle(Vector3.right, uLocalDirection, Vector3.forward);

//                        textureMatrices[i] = Matrix4x4.TRS(
//                            uvTranslation,
//                            Quaternion.identity,
//                            uvScale);
//                    }

//                    i++;
//                }

//                GameObject brushGO = new GameObject("Brush");
//                CSGBrush brush = RealtimeCSG.Legacy.BrushFactory.CreateBrushFromPlanes(
//                    brushGO,
//                    planes,
//                    null,
//                    null,
//                    materials,
//                    textureMatrices,
//                    RealtimeCSG.Legacy.TextureMatrixSpace.PlaneSpace,
//                    null,
//                    flags);

//                if (brush == null || brush.Shape == null)
//                {
//                    GameObject.DestroyImmediate(brushGO);
//                    return null;
//                }

//                for (int surfaceIndex = 0; surfaceIndex < brush.Shape.Surfaces.Length; surfaceIndex++)
//                {
//                    //brush.Shape.TexGens[surfaceIndex].Translation
//                    brush.Shape.TexGens[surfaceIndex].RotationAngle = rotations[surfaceIndex];
//                }

//                if (disable == false && brush.Shape.TexGenFlags.All((RealtimeCSG.Legacy.TexGenFlags flags) => { return 
//                    flags.HasFlag(RealtimeCSG.Legacy.TexGenFlags.NoCollision) &&
//                    flags.HasFlag(RealtimeCSG.Legacy.TexGenFlags.NoRender);
//                }))
//                {
//                    disable = true;
//                }

//                if (layer == Layer.Skip)
//                    disable = true;

//                if (disable)
//                    brush.gameObject.SetActive(false);

//                UnityEngine.Profiling.Profiler.EndSample();
//                return brush;
//            }

//            Plane GetPlaneFromString(string text)
//            {
//                UnityEngine.Profiling.Profiler.BeginSample("GetPlaneFromString");
//                // (880 -320 96) (888 -320 96) (888 -320 224)

//                //Debug.Log(text);
//                Vector3[] points = new Vector3[3];

//                MatchCollection matches = regexMatchParentheses.Matches(text);
//                for (int m = 0; m < 3; m++)
//                {
//                    MatchCollection vectorMatches = regexMatchNumbers.Matches(matches[m].Value);
//                    for (int i = 0; i < 3; i++)
//                    {
//                        //Debug.Log(vectorMatches[i].Value);
//                        points[m][i] = float.Parse(vectorMatches[i].Value);
//                    }

//                    points[m] = Converter.SourceTransformPoint(points[m]);
//                }

//                Plane plane = new Plane(points[0], points[1], points[2]);

//                UnityEngine.Profiling.Profiler.EndSample();
//                return plane;
//            }

//            void GetUVData(string value, out Vector3 direction, out float translation, out float scale)
//            {
//                // [0 0.9945 0.1045 16] 1.2492
//                //MatchCollection matches = Regex.Matches(value, rMatchNumbers);
//                MatchCollection matches = regexMatchNumbers.Matches(value);

//                direction = new Vector3(float.Parse(matches[0].Value), float.Parse(matches[1].Value), float.Parse(matches[2].Value));
//                translation = float.Parse(matches[3].Value);
//                scale = float.Parse(matches[4].Value);
//            }

//            GameObject staticGO = CreateModels(vmf.World);
//            staticGO.name = "Static";
//            staticGO.transform.parent = vmfGO.transform;

//            int entityCount = vmf.Body.Count;
//            int entityCounter = 0;

//            foreach (IVNode node in vmf.Body)  // Brush/Point entities
//            {
//                UnityEditor.EditorUtility.DisplayProgressBar($"Importing {path}", "Creating entities", (float)entityCounter / entityCount);
//                entityCounter++;
//                if (node is VBlock block && node.Name == "entity")
//                {
//                    block.TryGetValue("classname", out string className);

//                    Vector3 position = Vector3.zero;
//                    Vector3 eulerAngles = Vector3.zero;
//                    Quaternion rotation = Quaternion.identity;

//                    if (block.TryGetValue("origin", out string originString))
//                    {
//                        MatchCollection coll = Regex.Matches(originString, rMatchNumbers);
//                        position = Converter.SourceTransformPoint(new Vector3(
//                            float.Parse(coll[0].Value),
//                            float.Parse(coll[1].Value),
//                            float.Parse(coll[2].Value)
//                            ));
//                        //position = new Vector3(
//                        //    float.Parse(coll[0].Value),
//                        //    float.Parse(coll[1].Value),
//                        //    float.Parse(coll[2].Value)
//                        //    ) * worldSpaceScaleFactor;
//                    }
//                    if (block.TryGetValue("angles", out string angles))
//                    {
//                        MatchCollection coll = Regex.Matches(angles, rMatchNumbers);

//                        eulerAngles = new Vector3(
//                            float.Parse(coll[0].Value),
//                            float.Parse(coll[1].Value),
//                            float.Parse(coll[2].Value)
//                            );

//                        //rotation = Quaternion.AngleAxis(-eulerAngles.x, Vector3.forward) * rotation;
//                        //rotation = Quaternion.AngleAxis(-eulerAngles.y, Vector3.up) * rotation;
//                        //rotation = Quaternion.AngleAxis(eulerAngles.z, Vector3.right) * rotation;

//                        rotation = rotation * Quaternion.AngleAxis(-eulerAngles.x, Vector3.forward);
//                        rotation = rotation * Quaternion.AngleAxis(-eulerAngles.y, Vector3.up);
//                        rotation = rotation * Quaternion.AngleAxis(eulerAngles.z, Vector3.right);
//                    }
//                    if (className == "func_detail")
//                    {
//                        GameObject brushGO = CreateModels(block);
//                        brushGO.name = "func_detail";
//                        brushGO.transform.parent = vmfGO.transform;
//                    }
//                    if (className.Contains("prop_"))
//                    {
//                        block.TryGetValue("model", out string modelName);
//                        if (USource.ResourceManager.TryImportAsset<GameObject>(new Location(modelName, Location.Type.Source), out GameObject resource, ResourceManager.ImportMode.ReadFromAssetDataBaseOrImport, true))
//                        {
//                            if (resource == null)
//                            {
//                                Debug.LogWarning($"Failed to load {modelName}");
//                                continue;
//                            }

//                            GameObject gameObject = UnityEditor.PrefabUtility.InstantiatePrefab(resource) as GameObject;
//                            gameObject.transform.position = position;
//                            gameObject.transform.rotation = rotation;
                                
//                            if (className == "prop_static")
//                            {
//                                gameObject.isStatic = true;
//                                gameObject.GetComponent<Renderer>().staticShadowCaster = true;

//                                Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
//                                for (int i = rigidbodies.Length - 1; i >= 0; i--)
//                                {
//                                    GameObject.DestroyImmediate(rigidbodies[i]);
//                                }
//                            }
//                        }
//                    }
//                    else if (className.Contains("light"))
//                    {
//                        if (block.TryGetValue("_light", out string lightString) == false)
//                            continue;

//                        MatchCollection coll = Regex.Matches(lightString, rMatchNumbers);

//                        Vector4 lightData = new Vector4(255, 255, 255, 200);
//                        for (int i = 0; i < Mathf.Min(coll.Count, 4); i++)
//                        {
//                            if (float.TryParse(coll[i].Value, out float result))
//                                lightData[i] = result;
//                        }

//                        Light light = new GameObject(className).AddComponent<Light>();
//                        if (className == "light_spot")
//                        {
//                            light.type = LightType.Spot;
//                            block.TryGetValue("_cone", out string lightConeString);
//                            block.TryGetValue("_inner_cone", out string lightInnerConeString);
//                            float cone = float.Parse(lightConeString);
//                            float innerCone = float.Parse(lightInnerConeString);

//                            light.spotAngle = cone * 2;
//                            light.innerSpotAngle = innerCone * 2;
//                        }
//                        else if (className == "light")
//                        {
//                            light.type = LightType.Point;
//                        }
//                        else if (className == "light_environment")
//                        {
//                            light.type = LightType.Directional;
//                            block.TryGetValue("pitch", out string pitchString);
//                            if (float.TryParse(pitchString, out float pitch))
//                            {
//                                eulerAngles = new Vector3(pitch, 0, 0);
//                            }
                            
//                        }

//                        light.shadows = LightShadows.Hard;
//                        light.lightmapBakeType = LightmapBakeType.Baked;
//                        light.intensity = lightData[3] / (light.type == LightType.Directional ? 100 : 40.0f);
//                        light.range = lightData[3] / 5.0f;
//                        light.color = new Color(
//                            lightData[0] / 255.0f,
//                            lightData[1] / 255.0f,
//                            lightData[2] / 255.0f);

//                        light.transform.position = position;
//                        light.transform.rotation = rotation * Quaternion.AngleAxis(90, Vector3.right);
//                    }
//                    else if (className == "env_cubemap")
//                    {
//                        UnityEngine.ReflectionProbe cubemap = new GameObject("env_cubemap").AddComponent<UnityEngine.ReflectionProbe>();
//                        cubemap.transform.position = position;
//                    }
//                }
//            }

//            CSGModelManager.BuildLightmapUvs(true);

//            vmf.World.TryGetValue("skyname", out string skyMaterialName);

//            UnityEngine.Material skyMaterial = new UnityEngine.Material(Shader.Find("Skybox/6 Sided"));

//            void DoSkyStuff(string sourceSide, string unitySide)
//            {
//                if (USource.ResourceManager.TryImportAsset<UnityEngine.Material>(new Location($"materials/skybox/{skyMaterialName}{sourceSide}.vmt", Location.Type.Source), out UnityEngine.Material texResource, ResourceManager.ImportMode.ReadFromAssetDataBaseOrImport, true))
//                {
//                    // Update texture importer to clamp texture
//                    texResource.mainTexture.wrapMode = TextureWrapMode.Clamp;
//                    VtfImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texResource.mainTexture)) as VtfImporter;
//                    textureImporter.wrapMode = TextureWrapMode.Clamp;
//                    skyMaterial.SetTexture(unitySide, texResource.mainTexture);
//                }
//            }

//            DoSkyStuff("lf", "_BackTex");
//            DoSkyStuff("rt", "_FrontTex");
//            DoSkyStuff("dn", "_DownTex");
//            DoSkyStuff("up", "_UpTex");
//            DoSkyStuff("ft", "_LeftTex");
//            DoSkyStuff("bk", "_RightTex");

//            UnityEngine.RenderSettings.skybox = skyMaterial;

//            UnityEditor.EditorUtility.ClearProgressBar();
//            //UnityEditor.AssetDatabase.StopAssetEditing();
//        }
//#endif
//    }
//}
