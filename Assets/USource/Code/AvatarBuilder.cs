using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace USource
{
    public static class AvatarBuilder
    {
        public static IReadOnlyDictionary<string, string> BoneMapping => boneMapping;
        static readonly Dictionary<string, string> boneMapping = new Dictionary<string, string>()
        {
            {"ValveBiped.Bip01_Pelvis", "Hips"},
            {"ValveBiped.Bip01_Spine2", "Chest"},
            {"ValveBiped.Bip01_Head1", "Head"},
            {"ValveBiped.Bip01_Spine", "Spine" },
            {"ValveBiped.Bip01_Spine4", "UpperChest" },
            {"ValveBiped.Bip01_Neck1", "Neck" },
            {"mixamorig:LeftHandIndex3", "Left Index Distal" },
            {"mixamorig:LeftHandIndex2", "Left Index Intermediate" },
            {"mixamorig:LeftHandIndex1", "Left Index Proximal" },
            {"mixamorig:LeftHandPinky3", "Left Little Distal" },
            {"mixamorig:LeftHandPinky2", "Left Little Intermediate" },
            {"mixamorig:LeftHandPinky1", "Left Little Proximal" },
            {"mixamorig:LeftHandMiddle3", "Left Middle Distal" },
            {"mixamorig:LeftHandMiddle2", "Left Middle Intermediate" },
            {"mixamorig:LeftHandMiddle1", "Left Middle Proximal" },
            {"mixamorig:LeftHandRing3", "Left Ring Distal" },
            {"mixamorig:LeftHandRing2", "Left Ring Intermediate" },
            {"mixamorig:LeftHandRing1", "Left Ring Proximal" },
            {"mixamorig:LeftHandThumb3", "Left Thumb Distal" },
            {"mixamorig:LeftHandThumb2", "Left Thumb Intermediate" },
            {"mixamorig:LeftHandThumb1", "Left Thumb Proximal" },
            {"ValveBiped.Bip01_L_Foot", "LeftFoot" },
            {"ValveBiped.Bip01_L_Hand", "LeftHand" },
            {"ValveBiped.Bip01_L_Forearm", "LeftLowerArm" },
            {"ValveBiped.Bip01_L_Calf", "LeftLowerLeg" },
            {"ValveBiped.Bip01_L_Clavicle", "LeftShoulder" },
            {"mixamorig:LeftToeBase", "LeftToes" },
            {"ValveBiped.Bip01_L_UpperArm", "LeftUpperArm" },
            {"ValveBiped.Bip01_L_Thigh", "LeftUpperLeg" },
            {"mixamorig:RightHandIndex3", "Right Index Distal" },
            {"mixamorig:RightHandIndex2", "Right Index Intermediate" },
            {"mixamorig:RightHandIndex1", "Right Index Proximal" },
            {"mixamorig:RightHandPinky3", "Right Little Distal" },
            {"mixamorig:RightHandPinky2", "Right Little Intermediate" },
            {"mixamorig:RightHandPinky1", "Right Little Proximal" },
            {"mixamorig:RightHandMiddle3", "Right Middle Distal" },
            {"mixamorig:RightHandMiddle2", "Right Middle Intermediate" },
            {"mixamorig:RightHandMiddle1", "Right Middle Proximal" },
            {"mixamorig:RightHandRing3", "Right Ring Distal" },
            {"mixamorig:RightHandRing2", "Right Ring Intermediate" },
            {"mixamorig:RightHandRing1", "Right Ring Proximal" },
            {"mixamorig:RightHandThumb3", "Right Thumb Distal" },
            {"mixamorig:RightHandThumb2", "Right Thumb Intermediate" },
            {"mixamorig:RightHandThumb1", "Right Thumb Proximal" },
            {"ValveBiped.Bip01_R_Foot", "RightFoot" },
            {"ValveBiped.Bip01_R_Hand", "RightHand" },
            {"ValveBiped.Bip01_R_Forearm", "RightLowerArm" },
            {"ValveBiped.Bip01_R_Calf", "RightLowerLeg" },
            {"ValveBiped.Bip01_R_Clavicle", "RightShoulder" },
            {"mixamorig:RightToeBase", "RightToes" },
            {"ValveBiped.Bip01_R_UpperArm", "RightUpperArm" },
            {"ValveBiped.Bip01_R_Thigh", "RightUpperLeg" },
        };
        static AvatarBuilder()
        {

        }
        public static Avatar CreateAvatar(GameObject go)
        {
            Avatar avatar = UnityEngine.AvatarBuilder.BuildHumanAvatar(go, CreateHumanDescription(go));
            return avatar;
        }

        public static HumanDescription CreateHumanDescription(GameObject avatarRoot)
        {
            HumanDescription description = new HumanDescription()
            {
                armStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false,
                legStretch = 0.05f,
                lowerArmTwist = 0.5f,
                lowerLegTwist = 0.5f,
                upperArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                skeleton = CreateSkeleton(avatarRoot),
                human = CreateHuman(avatarRoot),
            };
            return description;
        }

        //Create a SkeletonBone array out of an Avatar GameObject
        //This assumes that the Avatar as supplied is in a T-Pose
        //The local positions of its bones/joints are used to define this T-Pose
        private static SkeletonBone[] CreateSkeleton(GameObject avatarRoot)
        {
            List<SkeletonBone> skeleton = new List<SkeletonBone>();

            Transform[] avatarTransforms = avatarRoot.GetComponentsInChildren<Transform>();
            foreach (Transform avatarTransform in avatarTransforms)
            {
                SkeletonBone bone = new SkeletonBone()
                {
                    name = avatarTransform.name,
                    position = avatarTransform.localPosition,
                    rotation = avatarTransform.localRotation,
                    scale = avatarTransform.localScale
                };

                skeleton.Add(bone);
            }
            return skeleton.ToArray();
        }

        //Create a HumanBone array out of an Avatar GameObject
        //This is where the various bones/joints get associated with the
        //joint names that Unity understands. This is done using the
        //static dictionary defined at the top. 
        private static HumanBone[] CreateHuman(GameObject avatarRoot)
        {
            List<HumanBone> human = new List<HumanBone>();

            Transform[] avatarTransforms = avatarRoot.GetComponentsInChildren<Transform>();
            foreach (Transform avatarTransform in avatarTransforms)
            {
                if (boneMapping.TryGetValue(avatarTransform.name, out string humanName))
                {
                    HumanBone bone = new HumanBone
                    {
                        boneName = avatarTransform.name,
                        humanName = humanName,
                        limit = new HumanLimit()
                    };
                    bone.limit.useDefaultValues = true;

                    human.Add(bone);
                }
            }
            return human.ToArray();
        }
    }
}
