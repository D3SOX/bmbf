using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Version = SemanticVersioning.Version;

namespace BMBF.Patching
{
    public class PatchBuilder
    {
        private readonly PatchManifest _manifest;
        private readonly List<FileModification> _fileModifications = new List<FileModification>();

        private TagManager? _tagManager;
        private string? _signingCertificate;
        private bool _allowExistingTag;
        

        public PatchBuilder(string patcherName, Version patcherVersion, TagManager tagManager)
        {
            _manifest = new PatchManifest(patcherName, patcherVersion.ToString());
            _tagManager = tagManager;
        }

        /// <summary>
        /// Specifies the modloader type to save in the tag
        /// </summary>
        /// <param name="modloaderName">Name of the modloader</param>
        /// <param name="modloaderVersion">Semver of the modloader</param>
        public PatchBuilder WithModloader(string modloaderName, Version modloaderVersion)
        {
            _manifest.ModloaderName = modloaderName;
            _manifest.ModloaderVersion = modloaderVersion;
            return this;
        }

        /// <summary>
        /// Adds a file to patch
        /// </summary>
        /// <param name="apkFilePath">Path of the file within the APK</param>
        /// <param name="patchFileDelegate">Delegate used to modify the file's contents</param>
        public PatchBuilder PatchFile(string apkFilePath, PatchFileDelegate patchFileDelegate)
        {
            _fileModifications.Add(new FileModification(patchFileDelegate, apkFilePath));
            return this;
        }

        /// <summary>
        /// Adds a file to be added/replaced in the APK
        /// </summary>
        /// <param name="apkFilePath">Path of the file within the APK</param>
        /// <param name="overwriteMode">Settings for file overwriting</param>
        /// <param name="openFileDelegate">Delegate used to open the source file</param>
        public PatchBuilder ModifyFile(string apkFilePath, OverwriteMode overwriteMode, OpenFileDelegate openFileDelegate)
        {
            _fileModifications.Add(new FileModification(openFileDelegate, overwriteMode, apkFilePath));
            return this;
        }

        /// <summary>
        /// Disables adding the modded tag to this APK
        /// </summary>
        public PatchBuilder DisableTagging()
        {
            _tagManager = null;
            return this;
        }

        /// <summary>
        /// Sets whether or not patching will fail if a modded tag already exists within the APK
        /// </summary>
        /// <param name="allowExistingTag">If true, patching will not fail if a tag already exists</param>
        public PatchBuilder SetAllowExistingTag(bool allowExistingTag)
        {
            _allowExistingTag = allowExistingTag;
            return this;
        }

        /// <summary>
        /// Sets the certificate to sign the APK with after patching
        /// </summary>
        /// <param name="certificate">Certificate and private key, in PEM format</param>
        public PatchBuilder Sign(string certificate)
        {
            _signingCertificate = certificate;
            return this;
        }

        private async Task DoFileModifications(ZipArchive apkArchive, ILogger logger, CancellationToken ct)
        {
            foreach (FileModification fileModification in _fileModifications)
            {
                logger.Debug($"Modding {fileModification.ApkFilePath}");
                var fileEntry = apkArchive.GetEntry(fileModification.ApkFilePath);
                if (fileModification.GenerateFromSourceFile != null)
                {
                    if (fileEntry == null)
                    {
                        throw new FileNotFoundException($"Cannot patch file {fileModification.ApkFilePath} in APK - file does not exist");
                    }

                    // Load the file into a MemoryStream
                    await using var tempStream = new MemoryStream();
                    await using (var fileStream = fileEntry.Open())
                    {
                        await fileStream.CopyToAsync(tempStream, ct);
                    }
                    tempStream.Position = 0;
                    
                    // Delete the existing entry and create a new one to write to
                    fileEntry.Delete();
                    fileEntry = apkArchive.CreateEntry(fileModification.ApkFilePath);
                    await using var outputStream = fileEntry.Open();

                    // Patch the file and write to the APK
                    fileModification.GenerateFromSourceFile(tempStream, outputStream);
                    continue;
                }

                if (fileModification.OpenSourceFile != null)
                {
                    if (fileEntry == null && fileModification.OverwriteMode == OverwriteMode.MustExist)
                    {
                        throw new FileNotFoundException($"File {fileModification.ApkFilePath} did not already exist");
                    }

                    if (fileEntry != null)
                    {
                        if (fileModification.OverwriteMode == OverwriteMode.MustBeNew)
                        {
                            throw new InvalidOperationException($"File {fileModification.ApkFilePath} already existed");
                        }
                        // Delete the existing entry if allowed
                        fileEntry.Delete();
                    }

                    // Create a new entry (since any existing entry must've been deleted by this point), and copy our source file into it
                    await using var fileStream = apkArchive.CreateEntry(fileModification.ApkFilePath).Open();
                    await fileModification.OpenSourceFile().CopyToAsync(fileStream, ct);
                    continue;
                }

                // This should never happen
                throw new ArgumentException("File modification was not set to open file or patch file");
            }
        }

        /// <summary>
        /// Patches the APK with the given path with the options in this builder
        /// </summary>
        /// <param name="apkPath">Path of the APK to patch</param>
        /// <param name="logger">Logger to print information to during patching</param>
        /// <param name="ct">Token to cancel patching</param>
        public async Task Patch(string apkPath, ILogger logger, CancellationToken ct)
        {
            logger.Information($"Patching {Path.GetFileName(apkPath)}");
            using (var apkArchive = ZipFile.Open(apkPath, ZipArchiveMode.Update))
            {
                _manifest.ModifiedFiles = _fileModifications.Select(f => f.ApkFilePath).ToHashSet();
                
                // Actually modify the APK
                await DoFileModifications(apkArchive, logger, ct);
                
                // Add the tag to the APK if configured
                if (_tagManager != null)
                {
                    logger.Information("Tagging APK");
                    _tagManager.AddTag(apkArchive, _manifest, _allowExistingTag);
                }
            }
            
            if (_signingCertificate != null)
            {
                logger.Information("Signing APK");
                await ApkSigner.SignApk(apkPath, _signingCertificate, _manifest.PatcherName, ct);
            }
        }
    }
}