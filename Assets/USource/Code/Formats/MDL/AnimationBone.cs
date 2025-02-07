using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using USource.MathLib;

namespace USource.Formats.MDL
{
    public class AnimationBone
    {
        public byte Bone;
        public byte Flags;
        public int NumFrames;
        public Quaternion pQuat48;
        public Quaternion pQuat64;
        public Vector3 pVec48;
        public List<Vector3> FrameAngles;
        public List<Vector3> FramePositions;

        public AnimationBone(byte bone, byte flags, int numFrames)
        {
            Bone = bone;
            Flags = flags;
            NumFrames = numFrames;
            FramePositions = new List<Vector3>();
            FrameAngles = new List<Vector3>();
        }

        public void ReadData(UReader br)
        {
            var delta = (Flags & (byte)ModelFlags.STUDIO_ANIM_DELTA) > 0;

            if ((Flags & (byte)ModelFlags.STUDIO_ANIM_ANIMROT) > 0 && NumFrames > 0)
            {
                // Why is this so painful :(
                // Read the per-frame data using RLE, just like GoldSource models
                var startPos = br.BaseStream.Position;
                var offsets = br.ReadShortArray(3);
                var endPos = br.BaseStream.Position;
                var rotFrames = new List<float[]>();
                for (var i = 0; i < NumFrames; i++) rotFrames.Add(new float[] { 0, 0, 0 });
                for (var i = 0; i < 3; i++)
                {
                    if (i < 0 || i >= offsets.Length || offsets[i] == 0) continue;
                    int newPos = (int)startPos + offsets[i];
                    if (newPos < 0) continue;
                    br.BaseStream.Position = startPos + offsets[i];
                    var values = br.ReadAnimationFrameValues(NumFrames);
                    for (var f = 0; f < values.Length; f++)
                    {
                        rotFrames[f][i] = +values[f];
                        if (f > 0 && delta) rotFrames[f][i] += values[f - 1];
                    }
                }
                FrameAngles.AddRange(rotFrames.Select(x => new Vector3(x[0], x[1], x[2])));
                br.BaseStream.Position = endPos;
            }
            if ((Flags & (byte)ModelFlags.STUDIO_ANIM_ANIMPOS) > 0 && NumFrames > 0)
            {
                // Same as above, except for the position coordinate
                var startPos = br.BaseStream.Position;
                var offsets = br.ReadShortArray(3);
                var endPos = br.BaseStream.Position;
                var posFrames = new List<float[]>();
                for (var i = 0; i < NumFrames; i++) posFrames.Add(new float[] { 0, 0, 0 });
                for (var i = 0; i < 3; i++)
                {
                    if (offsets[i] == 0) continue;
                    if (startPos + offsets[i] < 0) continue;
                    br.BaseStream.Position = startPos + offsets[i];
                    var values = br.ReadAnimationFrameValues(NumFrames);
                    for (var f = 0; f < values.Length; f++)
                    {
                        posFrames[f][i] = +values[f];
                        if (f > 0 && delta) posFrames[f][i] += values[f - 1];
                    }
                }
                FramePositions.AddRange(posFrames.Select(x => new Vector3(x[0], x[1], x[2])));
                br.BaseStream.Position = endPos;
            }
            if ((Flags & (byte)ModelFlags.STUDIO_ANIM_RAWROT) > 0)
            {
                var quat48 = new Quaternion48();
                quat48.theXInput = br.ReadUInt16();
                quat48.theYInput = br.ReadUInt16();
                quat48.theZWInput = br.ReadUInt16();

                pQuat48 = quat48.quaternion;
            }
            if ((Flags & (byte)ModelFlags.STUDIO_ANIM_RAWROT2) > 0)
            {
                var quat64 = new Quaternion64();
                quat64.theBytes = br.ReadBytes(8);

                pQuat64 = quat64.quaternion;
            }
            if ((Flags & (byte)ModelFlags.STUDIO_ANIM_RAWPOS) > 0)
            {
                var vec48 = new Vector48();
                vec48.x = new Float16 { bits = br.ReadUInt16() };
                vec48.y = new Float16 { bits = br.ReadUInt16() };
                vec48.z = new Float16 { bits = br.ReadUInt16() };

                pVec48 = vec48.ToVector3();
            }
        }
    }
}
