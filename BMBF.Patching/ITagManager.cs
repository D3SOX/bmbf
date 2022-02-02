using System;
using System.IO.Compression;

namespace BMBF.Patching;

/// <summary>
/// Represents a tool for managing APK tags.
/// </summary>
public interface ITagManager
{
    /// <summary>
    /// Registers a file within the APK that will be detected as a tag.
    /// Useful for tags from patching tools that are not using this tag format.
    /// </summary>
    /// <param name="tagPath">Path of the legacy tag within the APK</param>
    /// <param name="manifestGetter">Used to generate the manifest for this tag</param>
    void RegisterLegacyTag(string tagPath, Func<PatchManifest> manifestGetter);

    /// <summary>
    /// Finds the tag on the given APK
    /// </summary>
    /// <param name="apkArchive">APK to find the tag of</param>
    /// <returns>In order: A modern tag, if found, a legacy tag, if found, if neither found then null</returns>
    PatchManifest? GetTag(ZipArchive apkArchive);

    /// <summary>
    /// Tags the given APK
    /// </summary>
    /// <param name="apkArchive">The archive to add the tag to</param>
    /// <param name="manifest">Manifest to tag the APK with</param>
    /// <param name="addToExistingTags">Whether or not to merge this manifest with an existing tag if found. Existing tags will error if this is false</param>
    /// <exception cref="InvalidOperationException">Thrown if the APK is already tagged, and <paramref name="addToExistingTags"/> is false</exception>
    void AddTag(ZipArchive apkArchive, PatchManifest manifest, bool addToExistingTags);
}
