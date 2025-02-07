using UnityEngine;

namespace USource.Formats.MDL
{
    //TODO
    public struct StudioHeader : ISourceObject
    {
        public int id;
        public int version;

        public int checksum;
        public string name;

        public int dataLength;

        public Vector3 eyeposition;
        public Vector3 illumposition;
        public Vector3 hull_min;
        public Vector3 hull_max;
        public Vector3 view_bbmin;
        public Vector3 view_bbmax;

        public StudioHDRFlags flags;

        // mstudiobone_t
        public int bone_count;
        public int bone_offset;

        // mstudiobonecontroller_t
        public int bonecontroller_count;
        public int bonecontroller_offset;

        // mstudiohitboxset_t
        public int hitbox_count;
        public int hitbox_offset;

        // mstudioanimdesc_t
        public int localanim_count;
        public int localanim_offset;

        // mstudioseqdesc_t
        public int localseq_count;
        public int localseq_offset;

        public int activitylistversion;
        public int eventsindexed;

        // mstudiotexture_t
        public int texture_count;
        public int texture_offset;

        public int texturedir_count;
        public int texturedir_offset;

        public int skinreference_count;
        public int skinrfamily_count;
        public int skinreference_index;

        // mstudiobodyparts_t
        public int bodypart_count;
        public int bodypart_offset;

        // mstudioattachment_t
        public int attachment_count;
        public int attachment_offset;

        public int localnode_count;
        public int localnode_index;
        public int localnode_name_index;

        // mstudioflexdesc_t
        public int flexdesc_count;
        public int flexdesc_index;

        // mstudioflexcontroller_t
        public int flexcontroller_count;
        public int flexcontroller_index;

        // mstudioflexrule_t
        public int flexrules_count;
        public int flexrules_index;

        // mstudioikchain_t
        public int ikchain_count;
        public int ikchain_index;

        // mstudiomouth_t
        public int mouths_count;
        public int mouths_index;

        // mstudioposeparamdesc_t
        public int localposeparam_count;
        public int localposeparam_index;

        public int surfaceprop_index;

        public int keyvalue_index;
        public int keyvalue_count;

        // mstudioiklock_t
        public int iklock_count;
        public int iklock_index;

        public float mass;
        public int contents;

        // mstudiomodelgroup_t
        public int includemodel_count;
        public int includemodel_index;

        public int virtualModel;
        // Placeholder for mutable-void*

        // mstudioanimblock_t
        public int animblocks_name_index;
        public int animblocks_count;
        public int animblocks_index;

        public int animblockModel;
        // Placeholder for mutable-void*

        public int bonetablename_index;

        public int vertex_base;
        public int offset_base;

        // Used with $constantdirectionallight from the QC
        // Model should have flag #13 set if enabled
        public byte directionaldotproduct;

        public byte rootLod;
        // Preferred rather than clamped

        // 0 means any allowed, N means Lod 0 -> (N-1)
        public byte numAllowedRootLods;

        public byte unused;
        public int unused2;

        // mstudioflexcontrollerui_t
        public int flexcontrollerui_count;
        public int flexcontrollerui_index;

        public void ReadToObject(UReader reader, int version = 0)
        {
            id = reader.ReadInt32();
            this.version = reader.ReadInt32();
            checksum = reader.ReadInt32();

            name = Converters.IConverter.ByteArrayToString(reader.ReadBytes(64));

            dataLength = reader.ReadInt32();
            eyeposition = reader.ReadVector3();
            illumposition = reader.ReadVector3();
            hull_min = reader.ReadVector3();
            hull_max = reader.ReadVector3();
            view_bbmax = reader.ReadVector3();
            view_bbmax = reader.ReadVector3();
            flags = (StudioHDRFlags)reader.ReadInt32();

            bone_count = reader.ReadInt32();
            bone_offset = reader.ReadInt32();

            bonecontroller_count = reader.ReadInt32();
            bonecontroller_offset = reader.ReadInt32();

            hitbox_count = reader.ReadInt32();
            hitbox_offset = reader.ReadInt32();

            localanim_count = reader.ReadInt32();
            localanim_offset = reader.ReadInt32();

            localseq_count = reader.ReadInt32();
            localseq_offset = reader.ReadInt32();

            activitylistversion = reader.ReadInt32();
            eventsindexed = reader.ReadInt32();

            texture_count = reader.ReadInt32();
            texture_offset = reader.ReadInt32();

            texturedir_count = reader.ReadInt32();
            texturedir_offset = reader.ReadInt32();

            skinreference_count = reader.ReadInt32();
            skinrfamily_count = reader.ReadInt32();
            skinreference_index = reader.ReadInt32();

            bodypart_count = reader.ReadInt32();
            bodypart_offset = reader.ReadInt32();

            attachment_count = reader.ReadInt32();
            attachment_offset = reader.ReadInt32();

            localnode_count = reader.ReadInt32();
            localnode_index = reader.ReadInt32();
            localnode_name_index = reader.ReadInt32();

            flexdesc_count = reader.ReadInt32();
            flexdesc_index = reader.ReadInt32();

            flexcontroller_count = reader.ReadInt32();
            flexcontroller_index = reader.ReadInt32();

            flexrules_count = reader.ReadInt32();
            flexrules_index = reader.ReadInt32();

            ikchain_count = reader.ReadInt32();
            ikchain_index = reader.ReadInt32();

            mouths_count = reader.ReadInt32();
            mouths_index = reader.ReadInt32();

            localposeparam_count = reader.ReadInt32();
            localposeparam_index = reader.ReadInt32();

            surfaceprop_index = reader.ReadInt32();

            keyvalue_index = reader.ReadInt32();
            keyvalue_count = reader.ReadInt32();

            iklock_count = reader.ReadInt32();
            iklock_index = reader.ReadInt32();

            mass = reader.ReadSingle();

            contents = reader.ReadInt32();
            includemodel_count = reader.ReadInt32();
            includemodel_index = reader.ReadInt32();

            virtualModel = reader.ReadInt32();

            animblocks_name_index = reader.ReadInt32();
            animblocks_count = reader.ReadInt32();
            animblocks_index = reader.ReadInt32();

            animblockModel = reader.ReadInt32();

            bonetablename_index = reader.ReadInt32();

            vertex_base = reader.ReadInt32();
            offset_base = reader.ReadInt32();

            directionaldotproduct = reader.ReadByte();
            rootLod = reader.ReadByte();
            numAllowedRootLods = reader.ReadByte();

            reader.Skip(5);  // unknown byte and int

            flexcontrollerui_count = reader.ReadInt32();
            flexcontrollerui_index = reader.ReadInt32();
        }
    }
}
