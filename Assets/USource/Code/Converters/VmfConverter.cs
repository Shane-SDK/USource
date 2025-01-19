using System.Collections.Generic;
using UnityEngine;
using VMFParser;
using System.Linq;
using USource.SourceAsset;
using System.Text;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Components;
#endif

namespace USource.Converters
{
    public class VmfConverter : Converter
    {
        Stream stream;
        public VmfConverter(string sourcePath, Stream stream) : base(sourcePath, stream)
        {
            this.stream = stream;
        }

        public override UnityEngine.Object CreateAsset(ImportContext ctx)
        {
#if UNITY_EDITOR
            return CreateFromVMF(stream, ctx);
#else
            return new GameObject();
#endif
        }
#if UNITY_EDITOR
        // Load VMF
        public static GameObject CreateFromVMF(System.IO.Stream stream, ImportContext ctx)
        {
            GameObject vmfGO = GameObject.Find("VMF");
            {
                if (vmfGO != null)
                    Object.DestroyImmediate(vmfGO);
            }

            vmfGO = new GameObject("VMF");
            VMFParser.VMF vmf = new VMFParser.VMF(VmfAsset.ReadLines(() => stream, Encoding.UTF8).ToArray());

            // For each model (set of CSG brushes)
            // - Solid, transparent, and clipping models

            GameObject CreateCSGModel(VBlock block)
            {
                GameObject root = new GameObject();
                root.transform.parent = vmfGO.transform;
                root.isStatic = true;
                Dictionary<VMFLayer, CSGModel> models = new Dictionary<VMFLayer, CSGModel>();

                foreach (IVNode node in block.Body)
                {
                    if (node is VBlock childBlock)
                    {
                        CSGBrush brush = CreateCSGBrush(childBlock, out VMFLayer layer);

                        if (brush == null)
                            continue;

                        // Ensure CSG model actually exists
                        if (models.TryGetValue(layer, out CSGModel model) == false)
                        {
                            model = new GameObject(layer.ToString()).AddComponent<CSGModel>();
                            model.Settings |= ModelSettingsFlags.TwoSidedShadows;
                            model.transform.parent = root.transform;
                            model.ReceiveGI = ReceiveGI.Lightmaps;
                            model.gameObject.isStatic = true;

                            models[layer] = model;
                        }

                        brush.transform.parent = model.transform;
                    }
                }

                return root;
            }

            CSGBrush CreateCSGBrush(VBlock block, out VMFLayer layer)
            {
                layer = VMFLayer.Solid;
                bool disable = false;

                // Create CSG Brush
                int planeCount = block.Body.Count((node) => { return node.Name == "side"; });  // Count how many planes will be in the brush

                if (planeCount < 4)
                    return null;

                Plane[] planes = new Plane[planeCount];
                UnityEngine.Material[] materials = new UnityEngine.Material[planeCount];
                Matrix4x4[] textureMatrices = new Matrix4x4[planeCount];
                float[] rotations = new float[planeCount];
                RealtimeCSG.Legacy.TexGenFlags[] flags = new RealtimeCSG.Legacy.TexGenFlags[planeCount];
                int i = 0;

                // "uaxis" "[0 -1 0 -160] 0.25"
                // "vaxis" "[0 0 -1 288] 0.25"

                foreach (VBlock side in block.Body.Where((node) => { return node.Name == "side"; }))
                {
                    // Get plane
                    if (side.TryGetValue("plane", out string planeString) && GetPlaneFromString(planeString, out Plane plane))
                        planes[i] = plane;
                    else return null;

                    // Get material
                    if (side.TryGetValue("material", out string materialPath) == false) { i++; continue; }  // Material does not exist

                    if (USource.noCreateMaterials.Contains(materialPath.ToLower()))
                        return null;
                    if (USource.noRenderMaterials.Contains(materialPath.ToLower()))
                        flags[i] |= (RealtimeCSG.Legacy.TexGenFlags.NoRender | RealtimeCSG.Legacy.TexGenFlags.NoCastShadows);

                    Location location = new Location($"materials/{materialPath}.vmt", Location.Type.Source);


                    //if (ResourceManager.TryResolveMaterialPath(location.SourcePath, out Location resolvedMaterial))
                    //{
                    //    location = resolvedMaterial;
                    //}

                    if (!USource.ResourceManager.GetUnityObject(location, out UnityEngine.Material resourceMaterial, ctx.ImportMode, true))
                        resourceMaterial = Resources.Load<UnityEngine.Material>("Error");

                    materials[i] = resourceMaterial;

                    Vector2 uvScale = Vector2.one;
                    Vector2 uvTranslation = Vector2.zero;
                    Vector3 uWorldDirection = default;
                    Vector3 vWorldDirection = default;

                    for (int u = 0; u < 2; u++)
                    {
                        string axisKey = u == 0 ? "uaxis" : "vaxis";
                        side.TryGetValue(axisKey, out string uvData);
                        GetUVData(uvData, out Vector3 direction, out float translation, out float scale);

                        // translation : how many hammer units to adjust uv
                        // scale : uv scale
                        // uvRotation : WORLD SPACE uv direction

                        // Source translation: pixels to adjust texture by
                        // Source scaling: size of the texture - scaling of 1 means one pixel is one hammer unit in size

                        // Unity translation: normalized amount of texture to offset by
                        // Unity scaling: size of the texture

                        // Scale texture based on image size
                        int imageSize = 512;
                        if (resourceMaterial != null && resourceMaterial.HasProperty("_MainTex") && resourceMaterial.mainTexture != null)
                            imageSize = u == 0 ? resourceMaterial.mainTexture.width : resourceMaterial.mainTexture.height;

                        float sizeFactor = imageSize / 512.0f;

                        uvScale[u] = Converter.uvScaleFactor / (scale * 16) / sizeFactor;
                        uvTranslation[u] = translation / imageSize;

                        if (u == 0)
                            uWorldDirection = Converter.AxisConvertSource(direction);
                        else
                            vWorldDirection = Converter.AxisConvertSource(direction);
                    }

                    // Convert uv directions from world to local space
                    Vector3 uvWorldCross = Vector3.Cross(vWorldDirection, uWorldDirection);
                    //Quaternion worldToLocalRotation = Quaternion.FromToRotation(uvWorldCross, Vector3.forward);
                    Quaternion worldToLocalRotation = Quaternion.Inverse(Quaternion.LookRotation(uvWorldCross));
                    Vector3 uLocalDirection = worldToLocalRotation * uWorldDirection;
                    Vector3 vLocalDirection = worldToLocalRotation * vWorldDirection;

                    uvScale.x *= -Mathf.Sign(uLocalDirection.x);
                    uvScale.y *= -Mathf.Sign(vLocalDirection.y);

                    uvTranslation.x *= Mathf.Sign(uLocalDirection.x);
                    uvTranslation.y *= Mathf.Sign(vLocalDirection.y);

                    rotations[i] = Vector3.SignedAngle(Vector3.right, uLocalDirection, Vector3.forward);

                    textureMatrices[i] = Matrix4x4.TRS(
                        uvTranslation,
                        Quaternion.identity,
                        uvScale);

                    i++;
                }

                string brushName = block.TryGetValue("id", out string brushId) ? brushId : "brush";
                GameObject brushGO = new GameObject(brushName);
                CSGBrush brush = RealtimeCSG.Legacy.BrushFactory.CreateBrushFromPlanes(
                    brushGO,
                    planes,
                    null,
                    null,
                    materials,
                    textureMatrices,
                    RealtimeCSG.Legacy.TextureMatrixSpace.PlaneSpace,
                    null,
                    flags);

                if (brush == null || brush.Shape == null)
                {
                    Object.DestroyImmediate(brushGO);
                    return null;
                }

                for (int surfaceIndex = 0; surfaceIndex < brush.Shape.Surfaces.Length; surfaceIndex++)
                {
                    //brush.Shape.TexGens[surfaceIndex].Translation
                    brush.Shape.TexGens[surfaceIndex].RotationAngle = rotations[surfaceIndex];
                }

                if (disable == false && brush.Shape.TexGenFlags.All((flags) =>
                {
                    return
                    flags.HasFlag(RealtimeCSG.Legacy.TexGenFlags.NoCollision) &&
                    flags.HasFlag(RealtimeCSG.Legacy.TexGenFlags.NoRender);
                }))
                {
                    disable = true;
                }

                if (layer == VMFLayer.Skip)
                    disable = true;

                if (disable)
                    brush.gameObject.SetActive(false);

                return brush;
            }

            bool GetPlaneFromString(string text, out Plane plane)
            {
                plane = default;
                // (880 -320 96) (888 -320 96) (888 -320 224)

                string stripped = text.Replace("(", string.Empty).Replace(")", string.Empty);
                string[] split = stripped.Split(' ');
                if (split.Length < 9) return false;
                Vector3[] points = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    Vector3 vec3 = default;
                    for (int c = 0; c < 3; c++)
                    {
                        if (float.TryParse(split[i * 3 + c], out float floatValue))
                            vec3[c] = floatValue;
                        else return false;
                    }
                    points[i] = Converter.SourceTransformPoint(vec3);
                }

                plane = new Plane(points[0], points[1], points[2]);
                return true;
            }

            bool GetUVData(string value, out Vector3 direction, out float translation, out float scale)
            {
                direction = default; translation = default; scale = default;
                // [0 0.9945 0.1045 16] 1.2492
                string stripped = value.Replace("[", string.Empty).Replace("]", string.Empty);
                string[] split = stripped.Split(' ');
                if (split.Length < 5) return false;

                if (float.TryParse(split[0], out float v0) && float.TryParse(split[1], out float v1) && float.TryParse(split[2], out float v2))
                    direction = new Vector3(v0, v1, v2);
                else return false;

                if (!float.TryParse(split[3], out translation)) return false;
                if (!float.TryParse(split[4], out scale)) return false;

                return true;
            }

            GameObject staticGO = CreateCSGModel(vmf.World);
            staticGO.name = "World";
            staticGO.transform.parent = vmfGO.transform;

            int entityCount = vmf.Body.Count;
            int entityCounter = 0;


            // GO through Brush/Point entities

            foreach (VBlock block in vmf.Body
                .WhereClass<VBlock>()
                .Where(e => e.Name == "entity"))
            {
                block.TryGetValue("id", out string entityId);
                block.TryGetValue("classname", out string className);
                string goName = $"{className} [{entityId}]";

                Vector3 position = Vector3.zero;
                Vector3 eulerAngles = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                if (block.TryGetValue("origin", out position))
                    position = Converter.SourceTransformPoint(position);
                if (block.TryGetValue("angles", out eulerAngles))
                {
                    if (block.TryGetValue("pitch", out float pitch))
                        eulerAngles.x = pitch;

                    rotation = rotation * Quaternion.AngleAxis(-eulerAngles.x, Vector3.forward);
                    rotation = rotation * Quaternion.AngleAxis(-eulerAngles.y, Vector3.up);
                    rotation = rotation * Quaternion.AngleAxis(eulerAngles.z, Vector3.right);
                }

                if (IsBrushEntity(className))
                {
                    GameObject brushEntity = CreateCSGModel(block);
                    brushEntity.name = goName;
                }
                else  // Point entity
                {
                    // Try to use a Prefab/Mdl for the GameObject, unless no model value is provided
                    GameObject entityGO = null;
                    if (block.TryGetValue("model", out string modelValue) &&
                        USource.ResourceManager.GetUnityObject(new Location(modelValue, Location.Type.Source), out GameObject prefab, ctx.ImportMode))
                    {
                        // Instantiate prefab
#if UNITY_EDITOR
                        if (ctx.ImportMode == ImportMode.AssetDatabase)
                            entityGO = PrefabUtility.InstantiatePrefab(prefab, vmfGO.transform) as GameObject;
#endif
                        if (ctx.ImportMode == ImportMode.CreateAndCache)
                            entityGO = Object.Instantiate(prefab, vmfGO.transform);

                        entityGO.name = goName;

                        if (className == "prop_static")
                        {
                            entityGO.isStatic = true;
                            entityGO.GetComponent<Renderer>().staticShadowCaster = true;

                            Rigidbody[] rigidbodies = entityGO.GetComponentsInChildren<Rigidbody>();
                            for (int i = rigidbodies.Length - 1; i >= 0; i--)
                            {
                                rigidbodies[i].isKinematic = true;
                            }
                        }
                    }
                    else  // Point entity has no model, use empty GO
                    {
                        entityGO = new GameObject(goName);
                        entityGO.transform.parent = vmfGO.transform;
                    }

                    entityGO.transform.position = position;
                    entityGO.transform.rotation = rotation;

                    if (className == "env_cubemap")
                    {
                        entityGO.AddComponent<ReflectionProbe>();
                    }
                    else if (className.Contains("light") && block.TryGetValue("_light", out Vector4 lightValues))
                    {
                        Light light = entityGO.AddComponent<Light>();
                        light.shadows = LightShadows.Hard;
                        light.lightmapBakeType = LightmapBakeType.Baked;
                        light.intensity = lightValues[3] / (light.type == LightType.Directional ? 100 : 40.0f);
                        light.range = lightValues[3] / 5.0f;
                        light.color = new Color(
                            lightValues[0] / 255.0f,
                            lightValues[1] / 255.0f,
                            lightValues[2] / 255.0f);

                        light.transform.rotation = rotation * Quaternion.AngleAxis(90, Vector3.right);

                        if (className == "light_spot")
                        {
                            light.type = LightType.Spot;
                            if (block.TryGetValue("_cone", out float cone))
                                light.spotAngle = cone * 2;
                            if (block.TryGetValue("_inner_cone", out float innerCone))
                                light.innerSpotAngle = innerCone * 2;
                        }
                        else if (className == "light")
                        {
                            light.type = LightType.Point;
                        }
                        else if (className == "light_environment")
                        {
                            light.type = LightType.Directional;
                        }
                    }
                }
            }

            CSGModelManager.BuildLightmapUvs(true);

            return vmfGO;
        }
#endif
        public static bool IsBrushEntity(string className)
        {
            if (className == "func_detail") return true;
            return false;
        }
    }
}
