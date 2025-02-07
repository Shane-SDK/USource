namespace USource.Formats.MDL
{
    public struct StudioAnimDesc : ISourceObject
    {
        public int baseptr;
        public int sznameindex;

        public float fps;      // frames per second	
        public int flags;     // looping/non-looping flags

        public int numframes;

        // piecewise movement
        public int nummovements;
        public int movementindex;

        public int[] unused1;         // remove as appropriate (and zero if loading older versions)	

        public int animblock;
        public int animindex;  // non-zero when anim data isn't in sections

        public int numikrules;
        public int ikruleindex;   // non-zero when IK data is stored in the mdl
        public int animblockikruleindex; // non-zero when IK data is stored in animblock file

        public int numlocalhierarchy;
        public int localhierarchyindex;

        public int sectionindex;
        public int sectionframes; // number of frames used in each fast lookup section, zero if not used

        public short zeroframespan; // frames per span
        public short zeroframecount; // number of spans
        public int zeroframeindex;
        public float zeroframestalltime;       // saved during read stalls

        public void ReadToObject(UReader reader, int version = 0)
        {
            baseptr = reader.ReadInt32();
            sznameindex = reader.ReadInt32();
            fps = reader.ReadSingle();
            flags = reader.ReadInt32();
            numframes = reader.ReadInt32();
            nummovements = reader.ReadInt32();
            movementindex = reader.ReadInt32();

            reader.Skip(4 * 6);  // unused int array size 6

            animblock = reader.ReadInt32();
            animindex = reader.ReadInt32();

            numikrules = reader.ReadInt32();
            ikruleindex = reader.ReadInt32();
            animblockikruleindex = reader.ReadInt32();

            numlocalhierarchy = reader.ReadInt32();
            localhierarchyindex = reader.ReadInt32();

            sectionindex = reader.ReadInt32();
            sectionframes = reader.ReadInt32();

            zeroframespan = reader.ReadInt16();
            zeroframecount = reader.ReadInt16();
            zeroframeindex = reader.ReadInt32();
            zeroframestalltime = reader.ReadSingle();
        }
    }
}
