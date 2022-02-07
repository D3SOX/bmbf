using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Version = SemanticVersioning.Version;

namespace BMBF.Patching
{

    public class PatchBuilder : IPatchBuilder
    {
        private readonly PatchManifest _manifest;
        private readonly List<FileModification> _fileModifications = new();

        private ITagManager? _tagManager;
        private readonly IApkSigner _apkSigner;
        private string? _signingCertificate;
        private bool _allowExistingTag;


        /// <summary>
        /// Creates a new <see cref="PatchBuilder"/>
        /// </summary>
        /// <param name="patcherName">Name of the patcher that will be reported in the tag</param>
        /// <param name="patcherVersion">Version of the patcher that will be reported in the tag</param>
        /// <param name="tagManager">If not null, this overrides the default implementation of <see cref="ITagManager"/>
        /// </param>
        /// <param name="apkSigner">If not null, this overrides the default implementation of <see cref="IApkSigner"/></param>
        public PatchBuilder(string patcherName, Version patcherVersion, ITagManager? tagManager = null, IApkSigner? apkSigner = null)
        {
            _manifest = new PatchManifest(patcherName, patcherVersion.ToString());
            _tagManager = tagManager ?? new TagManager();
            _apkSigner = apkSigner ?? new ApkSigner();
        }
        
        public IPatchBuilder WithModloader(string modloaderName, Version modloaderVersion)
        {
            _manifest.ModloaderName = modloaderName;
            _manifest.ModloaderVersion = modloaderVersion;
            return this;
        }
        
        public IPatchBuilder PatchFile(string apkFilePath, PatchFileDelegate patchFileDelegate)
        {
            _fileModifications.Add(new FileModification(patchFileDelegate, apkFilePath));
            return this;
        }
        
        public IPatchBuilder ModifyFile(string apkFilePath, OverwriteMode overwriteMode, GetFileDelegate getFileDelegate)
        {
            _fileModifications.Add(new FileModification(getFileDelegate, overwriteMode, apkFilePath));
            return this;
        }
        
        public IPatchBuilder ModifyFileAsync(string apkFilePath, OverwriteMode overwriteMode, GetFileAsyncDelegate getFileDelegate)
        {
            _fileModifications.Add(new FileModification(getFileDelegate, overwriteMode, apkFilePath));
            return this;
        }
        
        public IPatchBuilder DisableTagging()
        {
            _tagManager = null;
            return this;
        }
        
        public IPatchBuilder SetAllowExistingTag(bool allowExistingTag)
        {
            _allowExistingTag = allowExistingTag;
            return this;
        }
        
        public IPatchBuilder Sign(string certificate)
        {
            _signingCertificate = certificate;
            return this;
        }

        private async Task DoFileModifications(ZipArchive apkArchive, ILogger logger, CancellationToken ct)
        {
            foreach (var fileModification in _fileModifications)
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

                if (fileModification.GetSourceFile != null || fileModification.GetSourceFileAsync != null)
                {
                    if (fileEntry == null && fileModification.OverwriteMode == OverwriteMode.MustExist)
                    {
                        throw new PatchingException($"File {fileModification.ApkFilePath} did not already exist");
                    }

                    if (fileEntry != null)
                    {
                        if (fileModification.OverwriteMode == OverwriteMode.MustBeNew)
                        {
                            throw new PatchingException($"File {fileModification.ApkFilePath} already existed");
                        }
                    }

                    // Create a new entry (since any existing entry must've been deleted by this point), and copy our source file into it
                    Stream? sourceFile = null;
                    if (fileModification.GetSourceFile != null)
                    {
                        sourceFile = fileModification.GetSourceFile();
                    }
                    else if(fileModification.GetSourceFileAsync != null)
                    {
                        sourceFile = await fileModification.GetSourceFileAsync(ct);
                    }
                    
                    if (sourceFile == null)
                    {
                        logger.Information($"File modification {fileModification.ApkFilePath} skipped");
                        return;
                    }

                    using var chosenSourceFile = sourceFile; // Make sure that the file gets disposed
                    
                    fileEntry?.Delete(); // Remove the existing entry
                    await using var fileStream = apkArchive.CreateEntry(fileModification.ApkFilePath).Open();
                    await sourceFile.CopyToAsync(fileStream, ct);
                    continue;
                }

                // This should never happen
                throw new ArgumentException("File modification was not set to open file or patch file");
            }
        }
        
        public async Task PatchAsync(IFileSystem fileSystem, string apkPath, ILogger logger, CancellationToken ct)
        {
            logger.Information($"Patching {Path.GetFileName(apkPath)}");
            using (var apkStream = fileSystem.File.OpenWrite(apkPath))
            using (var apkArchive = new ZipArchive(apkStream, ZipArchiveMode.Update))
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
                logger.Information("Disposing archive (this takes a minute)");
            }

            if (_signingCertificate != null)
            {
                logger.Information("Signing APK");
                await _apkSigner.SignApkAsync(fileSystem, apkPath, _signingCertificate, _manifest.PatcherName, ct);
            }
        }
    }
}
