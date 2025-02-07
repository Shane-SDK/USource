using UnityEngine;

namespace USource.Formats.MDL
{
    public struct StudioSeqDesc : ISourceObject
    {
        public int baseptr;

        public int szlabelindex;

        public int szactivitynameindex;

        public int flags;     // looping/non-looping flags

        public int activity;  // initialized at loadtime to game DLL values
        public int actweight;

        public int numevents;
        public int eventindex;

        public Vector3 bbmin;       // per sequence bounding box
        public Vector3 bbmax;

        public int numblends;

        // Index into array of shorts which is groupsize[0] x groupsize[1] in length
        public int animindexindex;

        public int movementindex; // [blend] float array for blended movement
        public int[] groupsize;
        public int[] paramindex;  // X, Y, Z, XR, YR, ZR
        public float[] paramstart; // local (0..1) starting value
        public float[] paramend;   // local (0..1) ending value
        public int paramparent;

        public float fadeintime;       // ideal cross fate in time (0.2 default)
        public float fadeouttime;  // ideal cross fade out time (0.2 default)

        public int localentrynode;        // transition node at entry
        public int localexitnode;     // transition node at exit
        public int nodeflags;     // transition rules

        public float entryphase;       // used to match entry gait
        public float exitphase;        // used to match exit gait

        public float lastframe;        // frame that should generation EndOfSequence

        public int nextseq;       // auto advancing sequences
        public int pose;          // index of delta animation between end and nextseq

        public int numikrules;

        public int numautolayers; //
        public int autolayerindex;

        public int weightlistindex;

        // FIXME: make this 2D instead of 2x1D arrays
        public int posekeyindex;

        public int numiklocks;
        public int iklockindex;

        // Key values
        public int keyvalueindex;
        public int keyvaluesize;

        public int cycleposeindex;        // index of pose parameter to use as cycle index

        public int activitymodifierindex;
        public int numactivitymodifiers;
        public void ReadToObject(UReader reader, int version = 0)
        {
            baseptr = reader.ReadInt32();
            szlabelindex = reader.ReadInt32();
            szactivitynameindex = reader.ReadInt32();
            flags = reader.ReadInt32();
            activity = reader.ReadInt32();
            actweight = reader.ReadInt32();
            numevents = reader.ReadInt32();
            eventindex = reader.ReadInt32();
            bbmin = reader.ReadVector3();
            bbmax = reader.ReadVector3();
            numblends = reader.ReadInt32();
            animindexindex = reader.ReadInt32();
            movementindex = reader.ReadInt32();
            groupsize = reader.ReadIntArray(2);
            paramindex = reader.ReadIntArray(2);
            paramstart = reader.ReadSingleArray(2);
            paramend = reader.ReadSingleArray(2);
            paramparent = reader.ReadInt32();
            fadeintime = reader.ReadSingle();
            fadeouttime = reader.ReadSingle();
            localentrynode = reader.ReadInt32();
            localexitnode = reader.ReadInt32();
            nodeflags = reader.ReadInt32();
            entryphase = reader.ReadSingle();
            exitphase = reader.ReadSingle();
            lastframe = reader.ReadSingle();
            nextseq = reader.ReadInt32();
            pose = reader.ReadInt32();
            numikrules = reader.ReadInt32();
            numautolayers = reader.ReadInt32();
            autolayerindex = reader.ReadInt32();
            weightlistindex = reader.ReadInt32();
            numiklocks = reader.ReadInt32();
            iklockindex = reader.ReadInt32();
            keyvalueindex = reader.ReadInt32();
            keyvaluesize = reader.ReadInt32();
            cycleposeindex = reader.ReadInt32();
            activitymodifierindex = reader.ReadInt32();
            numactivitymodifiers = reader.ReadInt32();
            reader.Skip(4 * 5);  // unused int array of size 5

        }
    }        //SEQUENCE
}
