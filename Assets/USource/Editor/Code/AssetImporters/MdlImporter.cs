using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using USource;
using System.IO;
using USource.Converters;
using USource.SourceAsset;
using System.Linq;
using UnityEngine.Rendering;

namespace USource.AssetImporters
{
    [ScriptedImporter(0, "mdl")]
    public class MdlImporter : ScriptedImporter
    {
        public Converters.ModelConverter.ImportOptions importOptions = ModelConverter.ImportOptions.Physics | ModelConverter.ImportOptions.Geometry;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
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
            DependencyTree dependencies = new(location);
            ISourceAsset sourceAsset = ISourceAsset.FromLocation(location);
            sourceAsset.GetDependencies(mdlStream, dependencies, false);
            foreach (Location dependency in dependencies.GetImmediateChildren(false))
                ctx.DependsOnArtifact(dependency.AssetPath);

            mdlStream.Close();
            mdlStream = File.OpenRead(ctx.assetPath);

            ModelConverter model = new ModelConverter(location.SourcePath, mdlStream, vvdStream, vtxStream, phyStream, importOptions);
            model.shadowCastingMode = shadowCastingMode;
            GameObject obj = model.CreateAsset( new ImportContext(ImportMode.AssetDatabase, ctx) ) as GameObject;

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
