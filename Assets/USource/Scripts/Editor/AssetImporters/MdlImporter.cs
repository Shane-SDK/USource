using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using USource;
using System.IO;
using USource.Converters;
namespace USource.AssetImporters
{
    [ScriptedImporter(0, "mdl")]
    public class MdlImporter : ScriptedImporter
    {
        [HideInInspector]
        public bool importHitboxes = false;
        public bool importPhysics = true;
        public bool importGeometry = true;
        [Header("WARNING: Animation Support is currently buggy and underdeveloped!")]
        public bool importAnimations = false;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // See if accompanying files exist (phys, triangles, vertices)
            Stream mdlStream = File.OpenRead(ctx.assetPath);

            bool TryGetStream(string extension, out Stream stream)
            {
                stream = null;
                string path = ctx.assetPath.Replace(".mdl", extension);
                if (File.Exists(path) == false)
                    return false;

                stream = File.OpenRead(path);
                return true;
            } 

            TryGetStream(".phy~", out Stream phyStream);
            TryGetStream(".vtx~", out Stream vtxStream);
            TryGetStream(".vvd~", out Stream vvdStream);


            Location location = new Location(ctx.assetPath, Location.Type.AssetDatabase);

            Model model = new Model(location.SourcePath, mdlStream, vvdStream, vtxStream, phyStream);
            foreach (string sourcePathDependency in model.GetSourceAssetDependencies())
            {
                Location dependentLocation = new Location(sourcePathDependency, Location.Type.Source);

                ctx.DependsOnArtifact(dependentLocation.AssetPath);
            }

            ResourceManager.ImportFlags flags = default;
            if (importHitboxes) flags |= ResourceManager.ImportFlags.Hitboxes;
            if (importPhysics) flags |= ResourceManager.ImportFlags.Physics;
            if (importAnimations) flags |= ResourceManager.ImportFlags.Animations;
            if (importGeometry) flags |= ResourceManager.ImportFlags.Geometry;

            GameObject obj = model.CreateAsset(flags) as GameObject;

            ctx.AddObjectToAsset("root", obj);
            if (obj.TryGetComponent(out MeshFilter filter))
                ctx.AddObjectToAsset("mesh", filter.sharedMesh);
            else if (obj.TryGetComponent(out SkinnedMeshRenderer renderer))
            {
                ctx.AddObjectToAsset("mesh", renderer.sharedMesh);

                // Attempt to export avatar

                //HumanDescription human = new HumanDescription();
                //human.human
                //Avatar avatar = AvatarBuilder.CreateAvatar(obj);
                //avatar.name = $"{obj.name} Avatar";
                //ctx.AddObjectToAsset("avatar", avatar);
            }
            MeshCollider[] meshColliders = obj.GetComponentsInChildren<MeshCollider>();
            for (int i = 0; i < meshColliders.Length; i++)
            {
                MeshCollider collider = meshColliders[i];
                ctx.AddObjectToAsset($"col.mesh.{i}", collider.sharedMesh);
            }
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                ctx.AddObjectToAsset($"col.{i}", collider);
            }

            if (model.clips != null)
            {
                for (int i = 0; i < model.clips.Count; i++)
                {
                    AnimationClip clip = model.clips[i];
                    ctx.AddObjectToAsset($"anim.{i}", clip);
                }
            }
            

            ctx.SetMainObject(obj);

            mdlStream?.Close();
            phyStream?.Close();
            vtxStream?.Close();
            vvdStream?.Close();
        }
    }
}
