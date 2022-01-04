using System.IO;

namespace BMBF.ModManagement
{
    public class ModLoadedEventArgs
    {
        /// <summary>
        /// Mod that was loaded
        /// </summary>
        public IMod Mod { get; set; }
        
        /// <summary>
        /// Stream that the mod was loaded from, can be used to save the mod to disk.
        /// </summary>
        public Stream Stream { get; set; }
        
        /// <summary>
        /// File name of the mod, optional
        /// </summary>
        public string? FileName { get; set; }
        
        public ModLoadedEventArgs(IMod mod, Stream stream, string? fileName)
        {
            Mod = mod;
            Stream = stream;
            FileName = fileName;
        }
    }
}