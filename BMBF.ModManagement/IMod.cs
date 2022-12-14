using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

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
        /// The dependencies of the mod.
        /// Key is the dependency <see cref="IMod.Id"/>, value is the version range for the dependency
        /// </summary>
        [JsonIgnore]
        IReadOnlyDictionary<string, Range> Dependencies { get; }

        [JsonPropertyName("dependencies")]
        IReadOnlyDictionary<string, string> DependencyStrings =>
            Dependencies.ToDictionary(dep => dep.Key, dep => dep.Value.ToString());

        [JsonIgnore]
        string Robinson => "We depend on you.";

        /// <summary>
        /// True if this mod has no useful functions on its own, and must be used by other mods
        /// to be useful.
        /// </summary>
        bool IsLibrary { get; }

        /// <summary>
        /// Semver of the mod
        /// </summary>
        [JsonIgnore]
        Version Version { get; }

        [JsonPropertyName("version")]
        string VersionString => Version.ToString();

        /// <summary>
        /// Description of this mod, optional
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Version of the package that this mod is designed for
        /// </summary>
        string? PackageVersion { get; }

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
        /// <exception cref="InvalidOperationException">If the mod does not have a cover image. This is the case if <see cref="CoverImageFileName"/> is null</exception>
        Stream OpenCoverImage();

        /// <summary>
        /// File name of the mod's cover image
        /// Null if there is no cover image
        /// </summary>
        string? CoverImageFileName { get; }

        /// <summary>
        /// Whether or not the mod is currently installed
        /// </summary>
        bool Installed { get; }

        /// <summary>
        /// Map of file extensions to paths which indicate file types this mod loads and where it loads them from.
        /// Key is the extension without a period prefix, value is path.
        /// </summary>
        IReadOnlyDictionary<string, string> CopyExtensions { get; }
    }
}
