using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMBF.Patching
{
    /// <summary>
    /// Utility for managing APK tags
    /// </summary>
    public class TagManager : ITagManager
    {
        private readonly string _tagLocation;
        private readonly Dictionary<string, Func<PatchManifest>> _legacyTags = new();

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Creates a new tag manager
        /// </summary>
        /// <param name="tagLocation">The path modern tags take within the APK</param>
        public TagManager(string tagLocation = "modded.json")
        {
            _tagLocation = tagLocation;
        }

        public void RegisterLegacyTag(string tagPath, Func<PatchManifest> manifestGetter)
        {
            _legacyTags[tagPath] = manifestGetter;
        }

        public PatchManifest? GetTag(ZipArchive apkArchive)
        {
            var tagEntry = apkArchive.GetEntry(_tagLocation);
            if (tagEntry == null)
            {
                // If no modern tag exists, and a legacy tag exists, use that
                var legacyTagPair = _legacyTags.FirstOrDefault(pair => apkArchive.GetEntry(pair.Key) != null);
                return legacyTagPair.Value?.Invoke();
            }

            using var tagStream = tagEntry.Open();
            return JsonSerializer.Deserialize<PatchManifest>(tagStream, _serializerOptions);
        }

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
            // Save the new tag to the APK
            JsonSerializer.Serialize(tagStream, manifest, _serializerOptions);
        }
    }
}
