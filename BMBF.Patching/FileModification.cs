
namespace BMBF.Patching
{
    /// <summary>
    /// Represents a file replaced in the APK
    /// </summary>
    public class FileModification
    {
        /// <summary>
        /// Function to open the source file for this replacement
        /// </summary>
        internal GetFileDelegate? GetSourceFile { get; }
        
        /// <summary>
        /// Async function to open the source file for this replacement
        /// </summary>
        internal GetFileAsyncDelegate? GetSourceFileAsync { get; }

        /// <summary>
        /// Function to generate this file from an existing file in the APK
        /// </summary>
        internal PatchFileDelegate? GenerateFromSourceFile { get; }

        /// <summary>
        /// Whether or not the file can be overwritten, or if it must be overwritten
        /// </summary>
        public OverwriteMode OverwriteMode { get; }

        /// <summary>
        /// Path in the APK that was modified
        /// </summary>
        public string ApkFilePath { get; }

        internal FileModification(GetFileDelegate getSourceFile, OverwriteMode overwriteMode, string apkFilePath)
        {
            GetSourceFile = getSourceFile;
            OverwriteMode = overwriteMode;
            ApkFilePath = apkFilePath;
        }
        
        internal FileModification(GetFileAsyncDelegate getSourceFileAsync, OverwriteMode overwriteMode, string apkFilePath)
        {
            GetSourceFileAsync = getSourceFileAsync;
            OverwriteMode = overwriteMode;
            ApkFilePath = apkFilePath;
        }

        internal FileModification(PatchFileDelegate generateFromSourceFile, string apkFilePath)
        {
            GenerateFromSourceFile = generateFromSourceFile;
            OverwriteMode = OverwriteMode.MustExist; // As we are generating this file from an existing one, it must already exist and will be overwritten
            ApkFilePath = apkFilePath;
        }
    }
}
