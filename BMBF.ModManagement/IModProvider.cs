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
        event EventHandler<IMod> ModLoaded;

        /// <summary>
        /// Invoked whenever a mod is deleted by the provider.
        /// The argument is the mod ID.
        /// </summary>
        event EventHandler<string> ModUnloaded;

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
        /// Parses a mod from the given stream, without importing it.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>The parsed mod, or null if the given stream was not a mod type this provider can load. Calling install/uninstall operations on this mod is not safe</returns>
        /// <exception cref="InstallationException">If the mod was the correct mod type for this provider, but had another format issue</exception>
        Task<IMod?> TryParseModAsync(Stream stream);
        
        /// <summary>
        /// Adds the given mod to the providers mod set.
        /// </summary>
        /// <param name="mod">Mod to add to the mod set</param>
        /// <exception cref="InstallationException">Any error while adding the mod</exception>
        Task AddModAsync(IMod mod);

        /// <summary>
        /// Unloads the given mod from the provider
        /// </summary>
        /// <param name="mod">The mod to unload</param>
        /// <returns>True if this mod was originally loaded by the provider, false otherwise</returns>
        Task<bool> UnloadModAsync(IMod mod);
    }
}