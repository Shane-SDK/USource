using USource.Converters;
#if UNITY_EDITOR
using UnityEditor.AssetImporters;
#endif

namespace USource
{
    public class ImportContext
    {
        public ImportContext(ImportMode mode)
        {
            this.importMode = mode;
        }
#if UNITY_EDITOR
        public ImportContext(ImportMode mode, AssetImportContext assetImportCtx)
        {
            this.assetImportContext = assetImportCtx;
            this.importMode = mode;
        }
#endif
#if UNITY_EDITOR
        public AssetImportContext AssetImportContext => assetImportContext;
        AssetImportContext assetImportContext;
#endif
        public ImportMode ImportMode => importMode;
        ImportMode importMode;
    }
}
