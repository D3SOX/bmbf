using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.ModManagement;

namespace BMBF.Backend.Services;

public interface IModService
{
    /// <summary>
    /// Invoked whenever a new mod is loaded
    /// </summary>
    event EventHandler<IMod> ModAdded;

    /// <summary>
    /// Invoked whenever a mod is removed. Argument is the mod ID
    /// </summary>
    event EventHandler<string> ModRemoved;

    /// <summary>
    /// Invoked whenever a mod's install status changes.
    /// </summary>
    event EventHandler<IMod> ModStatusChanged;

    /// <summary>
    /// Gets a dictionary of all currently loaded mods.
    /// </summary>
    /// <returns>A dictionary of all currently loaded mods.
    /// The key is the mod ID, value is a pair with the mod and the mod's full path</returns>
    Task<IReadOnlyDictionary<string, (IMod mod, string path)>> GetModsAsync();

    /// <summary>
    /// Attempts to import the given stream as a mod.
    /// </summary>
    /// <param name="stream">The stream containing the mod's data</param>
    /// <param name="fileName">File name of the mod</param>
    /// <returns>The result of importing the mod, or null if the given stream/filename do not constitute
    /// a mod type any of the registered providers can load.</returns>
    Task<FileImportResult?> TryImportModAsync(Stream stream, string fileName);

    /// <summary>
    /// Loads mods in the mod folder that haven't been loaded yet.
    /// </summary>
    Task LoadNewModsAsync();
}