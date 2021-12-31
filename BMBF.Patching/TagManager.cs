using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BMBF.Patching
{
    /// <summary>
    /// Utility for managing APK tags
    /// </summary>
    public class TagManager
    {
        private readonly string _tagLocation;
        private readonly Dictionary<string, Func<PatchManifest>> _legacyTags = new Dictionary<string, Func<PatchManifest>>();

        private readonly JsonSerializer _jsonSerializer = new JsonSerializer()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Creates a new tag manager
        /// </summary>
        /// <param name="tagLocation">The path modern tags within the APK</param>
        public TagManager(string tagLocation = "modded.json")
        {
            _tagLocation = tagLocation;
        }

        /// <summary>
        /// Registers a file within the APK that will be detected as a tag.
        /// Useful for tags from patching tools that are not using this tag format.
        /// </summary>
        /// <param name="tagPath">Path of the legacy tag within the APK</param>
        /// <param name="manifestGetter">Used to generate the manifest for this tag</param>
        public void RegisterLegacyTag(string tagPath, Func<PatchManifest> manifestGetter)
        {
            _legacyTags[tagPath] = manifestGetter;
        }

        /// <summary>
        /// Finds the tag on the given APK
        /// </summary>
        /// <param name="apkArchive">APK to find the tag of</param>
        /// <returns>In order: A modern tag, if found, a legacy tag, if found, if neither found then null</returns>
        public PatchManifest? GetTag(ZipArchive apkArchive)
        {
            var tagEntry = apkArchive.GetEntry(_tagLocation);
            if (tagEntry == null)
            {
                // If no modern tag exists, and a legacy tag exists, use that
                var legacyTagPair = _legacyTags.FirstOrDefault(pair => apkArchive.GetEntry(pair.Key) != null);
                return legacyTagPair.Value?.Invoke() ?? null;
            }
            
            using var tagStream = tagEntry.Open();
            using var tagReader = new StreamReader(tagStream);
            using var jsonReader = new JsonTextReader(tagReader);
            return _jsonSerializer.Deserialize<PatchManifest>(jsonReader);
        }

        /// <summary>
        /// Tags the given APK
        /// </summary>
        /// <param name="apkArchive">The archive to add the tag to</param>
        /// <param name="manifest">Manifest to tag the APK with</param>
        /// <param name="addToExistingTags">Whether or not to merge this manifest with an existing tag if found. Existing tags will error if this is false</param>
        /// <exception cref="InvalidOperationException">Thrown if the APK is already tagged, and <paramref name="addToExistingTags"/> is false</exception>
        public void AddTag(ZipArchive apkArchive, PatchManifest manifest, bool addToExistingTags)
        {
            var existingManifest = GetTag(apkArchive);
            if (existingManifest != null)
            {
                if (!addToExistingTags)
                {
                    throw new InvalidOperationException("Attempted to add a tag to an APK that was already tagged");
                }

                // Copy over the previous manifest's modified files
                foreach (var modifiedFile in existingManifest.ModifiedFiles)
                {
                    manifest.ModifiedFiles.Add(modifiedFile);
                }
                // Delete the existing tag
                apkArchive.GetEntry(_tagLocation)?.Delete(); // Tag entry must exist, as it was just loaded from
            }

            var tagEntry = apkArchive.CreateEntry(_tagLocation);
            using var tagStream = tagEntry.Open();
            using var writer = new StreamWriter(tagStream);
            using var jsonWriter = new JsonTextWriter(writer);
            // Save the new tag to the APK
            _jsonSerializer.Serialize(jsonWriter, manifest);
        }
    }
}