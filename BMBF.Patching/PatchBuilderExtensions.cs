using System.IO;
using QuestPatcher.Axml;

namespace BMBF.Patching
{
    public static class PatchBuilderExtensions
    {
        /// <summary>
        /// Adds a file to be added/overwritten in the APK
        /// </summary>
        /// <param name="patchBuilder">Builder to add the patch to</param>
        /// <param name="apkFilePath">Path of the file within the APK</param>
        /// <param name="filePath">Path of the file to add to the APK</param>
        /// <param name="overwriteMode">Whether or not to allow file overwriting</param>
        public static PatchBuilder ModifyFile(this PatchBuilder patchBuilder, string apkFilePath, string filePath, OverwriteMode overwriteMode)
        {
            patchBuilder.ModifyFile(apkFilePath, overwriteMode, () => File.OpenRead(filePath));
            return patchBuilder;
        }

        /// <summary>
        /// Utility method for patching the manifest 
        /// </summary>
        /// <param name="patchBuilder">Builder to add the patch to</param>
        /// <param name="manifestModDelegate">Delegate used to patch the manifest</param>
        public static PatchBuilder ModifyManifest(this PatchBuilder patchBuilder, ManifestModDelegate manifestModDelegate)
        {
            patchBuilder.PatchFile("AndroidManifest.xml", (readFrom, writeTo) =>
            {
                AxmlElement element = AxmlLoader.LoadDocument(readFrom);
                ApkManifest manifest = new ApkManifest(element);
                manifestModDelegate(manifest);

                AxmlSaver.SaveDocument(writeTo, element);
            });
            return patchBuilder;
        }
    }
}