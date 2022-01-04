using System;
using System.IO;
using System.Threading.Tasks;

namespace BMBF.ModManagement
{
    /// <summary>
    /// Represents a class which can load mods.
    /// NOTE: The implementation is responsible for disposing its mods when disposed
    /// </summary>
    public interface IModProvider : IDisposable
    {
        /// <summary>
        /// Invoked whenever a mod is imported by the provider
        /// </summary>
        event EventHandler<ModLoadedEventArgs> ModLoaded;

        /// <summary>
        /// Invoked whenever a mod is deleted by the provider
        /// </summary>
        event EventHandler<IMod> ModUnloaded;

        /// <summary>
        /// Invoked whenever a mod is installed or uninstalled
        /// </summary>
        event EventHandler<IMod> ModStatusChanged;

        /// <summary>
        /// Early check to see if this provider may be able to import a file with the given name
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>True if the provider may be able to import a file with this name, false if it definitely won't</returns>
        bool CanAttemptImport(string fileName);
        
        /// <summary>
        /// Attempts to import a mod.
        /// NOTE: If this operation was successful, the caller should not dispose the stream. Ownership of the stream
        /// is passed to the instance of <see cref="IModProvider"/>
        /// </summary>
        /// <param name="stream">Stream to import the mod from.</param>
        /// <param name="fileName">File name of the mod</param>
        /// <returns>The loaded mod, or null if the provider detected that the mod was not the correct type for this provider</returns>
        /// <exception cref="InstallationException">If the mod was the correct mod type for this provider, but had another format issue</exception>
        ValueTask<IMod?> TryImportModAsync(Stream stream, string fileName);

        /// <summary>
        /// Unloads the given mod from the provider
        /// </summary>
        /// <param name="mod">The mod to unload</param>
        /// <returns>True if this mod was originally loaded by the provider, false otherwise</returns>
        Task<bool> UnloadModAsync(IMod mod);
    }
}