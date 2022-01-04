﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace BMBF.ModManagement
{
    /// <summary>
    /// Represents a mod from a mod provider
    /// </summary>
    public interface IMod : IDisposable
    {
        /// <summary>
        /// Identifier for the mod. Must not contain whitespace
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Human-readable name of the mod
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Author of the mod, optional
        /// </summary>
        string? Author { get; }
        
        /// <summary>
        /// Person who ported this mod from another platform, optional
        /// </summary>
        string? Porter { get; }
        
        /// <summary>
        /// Version of the mod
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Description of this mod, optional
        /// </summary>
        string? Description { get; }
        
        /// <summary>
        /// Version of the package that this mod is designed for
        /// </summary>
        string PackageVersion { get; }

        /// <summary>
        /// Installs the mod
        /// <exception cref="InstallationException">Any error which occurred while installing the mod</exception>
        /// </summary>
        Task InstallAsync();

        /// <summary>
        /// Uninstalls the mod
        /// <exception cref="InstallationException">Any error which occurred while uninstalling the mod</exception>
        /// </summary>
        Task UninstallAsync();

        /// <summary>
        /// Opens the cover image of the mod
        /// </summary>
        /// <returns>A stream which can be used to read the cover image, or null if there is no cover image</returns>
        Stream? OpenCoverImage();
        
        /// <summary>
        /// Whether or not the mod is currently installed
        /// </summary>
        bool Installed { get; }
    }
}