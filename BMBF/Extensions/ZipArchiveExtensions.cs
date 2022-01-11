using System.IO;
using System.IO.Compression;

namespace BMBF.Extensions
{
    public static class ZipArchiveExtensions
    {
        /// <summary>
        /// Extracts the given archive to the given directory path.
        /// The directory will be created if it does not exist.
        /// Existing files will be overwritten
        /// </summary>
        /// <param name="archive">The archive to extract</param>
        /// <param name="path">The directory to extract the archive into</param>
        public static void ExtractToDirectory(this ZipArchive archive, string path)
        {
            foreach (var entry in archive.Entries)
            {
                var extractPath = Path.Combine(path, entry.FullName);
                var directoryName = Path.GetDirectoryName(extractPath);

                if (directoryName != null && !Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

                // Skip folder entries
                if (directoryName?.TrimEnd(Path.DirectorySeparatorChar) == extractPath.TrimEnd(Path.DirectorySeparatorChar)) continue;
                
                if(File.Exists(extractPath)) File.Delete(extractPath);
                using var outputStream = File.OpenWrite(extractPath);
                using var entryStream = entry.Open();
                entryStream.CopyTo(outputStream);
            }
        }
    }
}