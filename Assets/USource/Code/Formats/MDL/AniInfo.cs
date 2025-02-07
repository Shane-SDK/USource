using UnityEngine;
using System.Collections.Generic;

namespace USource.Formats.MDL
{
    public struct AniInfo
    {
        public string name;
        public StudioAnimDesc studioAnim;
        public List<AnimationBone> AnimationBones;
        public Keyframe[][] PosX;
        public Keyframe[][] PosY;
        public Keyframe[][] PosZ;

        public Keyframe[][] RotX;
        public Keyframe[][] RotY;
        public Keyframe[][] RotZ;
        public Keyframe[][] RotW;
    }
}
