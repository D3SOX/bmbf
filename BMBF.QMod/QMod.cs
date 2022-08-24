using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;
using Serilog;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace BMBF.QMod
{
    internal class QMod : IMod
    {
        public string Id => Mod.Id;
        public string Name => Mod.Name;
        public string Author => Mod.Author;
        public string? Porter => Mod.Porter;
        
        public IReadOnlyDictionary<string, Range> Dependencies { get; }
        public string? Description => Mod.Description;
        public string? PackageVersion => Mod.PackageVersion;

        public bool IsLibrary => Mod.IsLibrary;
        public Version Version => Mod.Version;

        public bool Installed
        {
            get => _installed;
            set
            {
                if (_installed != value)
                {
                    _installed = value;
                    _provider.InvokeModStatusChanged(this);
                }
            }
        }
        private bool _installed;
        
        public string? CoverImageFileName => Path.GetFileName(Mod.CoverImagePath);
        public IReadOnlyDictionary<string, string> CopyExtensions { get; }

        internal QuestPatcher.QMod.QMod Mod { get; }

        private IFileSystem FileSystem => _provider.FileSystem;
        private HttpClient HttpClient => _provider.HttpClient;
        private ILogger Logger => _provider.Logger;

        private readonly QModProvider _provider;

        private bool _disposed;

        public QMod(QuestPatcher.QMod.QMod mod, QModProvider provider)
        {
            Mod = mod;
            _provider = provider;
            // Verify that extensions are lower case
            CopyExtensions = mod.CopyExtensions.ToDictionary(c => c.Extension, c => c.Destination, StringComparer.OrdinalIgnoreCase);
            Dependencies = mod.Dependencies.ToDictionary(dep => dep.Id, dep => dep.VersionRange);
        }

        /// <summary>
        /// Creates a logger that will add indentation depending on the depth of the current mod within the install tree.
        /// </summary>
        /// <param name="depth">How deep the install/uninstall is. 0 is the root depth.</param>
        /// <returns>The created logger</returns>
        private ILogger CreateInstallLogger(int depth) =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new IndentationSink(Logger, depth * 4)) // 4 spaces per mod install
                .CreateLogger();
        
        private bool VerifyRegistered()
        {
            if (_provider.Mods.TryGetValue(Id, out var mod) && mod == this)
            {
                return true;
            }
            return false;
        }


        public async Task InstallAsync()
        {
            await _provider.ModManager.InstallLock.WaitAsync();
            try
            {
                if (!VerifyRegistered())
                {
                    return;
                }

                Logger.Information($"Installing {Id} v{Version}");
                await InstallAsyncInternal(new HashSet<string>());
            }
            finally
            {
                _provider.ModManager.InstallLock.Release();
            }
        }

        public async Task UninstallAsync()
        {
            await _provider.ModManager.InstallLock.WaitAsync();
            try
            {
                if (!VerifyRegistered())
                {
                    return;
                }

                Logger.Information($"Uninstalling {Id} v{Version}");
                await UninstallAsyncInternal();
            }
            finally
            {
                _provider.ModManager.InstallLock.Release();
            }
        }

        public Stream OpenCoverImage()
        {
            if (CoverImageFileName == null)
            {
                throw new InvalidOperationException("Cannot open cover image of mod without a cover image");
            }

            return Mod.OpenCoverImage();
        }

        internal void UpdateStatusInternal(bool notify)
        {
            if (notify)
            {
                Installed = FindStatusInternal();
            }
            else
            {
                _installed = FindStatusInternal();
            }
        }

        private bool FindStatusInternal()
        {
            // Check that all the mod files, library files, and file copies are installed
            foreach (string m in Mod.ModFileNames)
            {
                if (!FileSystem.File.Exists(Path.Combine(_provider.ModsPath, Path.GetFileName(m))))
                {
                    return false;
                }
            }

            foreach (string lib in Mod.LibraryFileNames)
            {
                if (!FileSystem.File.Exists(Path.Combine(_provider.LibsPath, Path.GetFileName(lib))))
                {
                    return false;
                }
            }

            foreach (var fileCopy in Mod.FileCopies)
            {
                if (!FileSystem.File.Exists(fileCopy.Destination))
                {
                    return false;
                }
            }

            return true;
        }

        internal async Task InstallAsyncInternal(HashSet<string> installPath)
        {
            var logger = CreateInstallLogger(installPath.Count);
            if (Installed) return;

            installPath.Add(Id);
            try
            {
                // Install dependencies first
                if (Mod.Dependencies.Count > 0)
                {
                    logger.Information($"Checking that {Mod.Dependencies.Count} dependencies are installed:");
                    foreach (var dependency in Mod.Dependencies)
                    {
                        await PrepareDependency(dependency, installPath, logger);
                    }
                }

                // Make sure that destination paths exist first
                FileSystem.Directory.CreateDirectory(_provider.ModsPath);
                FileSystem.Directory.CreateDirectory(_provider.LibsPath);

                // Copy over all of the mod files, lib files and file copies in order to actually install the mod

                foreach (var m in Mod.ModFileNames)
                {
                    string destPath = Path.Combine(_provider.ModsPath, Path.GetFileName(m));
                    await using var modStream = Mod.OpenModFile(m);
                    await ExtractAsync(modStream, destPath).ConfigureAwait(false);
                    logger.Debug($"Copied {m} to mods");
                }

                foreach (var lib in Mod.LibraryFileNames)
                {
                    string destPath = Path.Combine(_provider.LibsPath, Path.GetFileName(lib));
                    await using var libStream = Mod.OpenLibraryFile(lib);
                    await ExtractAsync(libStream, destPath).ConfigureAwait(false);
                    logger.Debug($"Copied {lib} to libs");
                }

                foreach (var fc in Mod.FileCopies)
                {
                    await using var copyStream = Mod.OpenFileCopy(fc);

                    // Create the directory if it does not already exist
                    var directoryName = Path.GetDirectoryName(fc.Destination);
                    if (directoryName != null)
                    {
                        FileSystem.Directory.CreateDirectory(directoryName);
                    }

                    await ExtractAsync(copyStream, fc.Destination).ConfigureAwait(false);
                    logger.Debug($"Copied {fc.Name} to {fc.Destination}");
                }
                logger.Information($"Installed {Mod.ModFileNames.Count} mod files, {Mod.LibraryFileNames.Count} library files and {Mod.FileCopies.Count} file copies");

                Installed = true;
            }
            finally
            {
                installPath.Remove(Id);
            }
        }

        internal async Task<List<QMod>> UninstallAsyncInternal(int depth = 0)
        {
            if (!Installed) return new List<QMod>();
            var logger = CreateInstallLogger(depth);

            // Uninstall ourself

            foreach (string m in Mod.ModFileNames)
            {
                string destPath = Path.Combine(_provider.ModsPath, Path.GetFileName(m));
                if (FileSystem.File.Exists(destPath))
                {
                    FileSystem.File.Delete(destPath);
                    logger.Debug($"Deleted {m} from mods");
                }
            }

            foreach (string lib in Mod.LibraryFileNames)
            {
                // Skip library files still in use by other libraries
                if (_provider.Mods.Values.Any(mod => mod.Id != Id && mod.Installed && mod.Mod.LibraryFileNames.Contains(lib))) continue;

                string destPath = Path.Combine(_provider.LibsPath, Path.GetFileName(lib));
                if (FileSystem.File.Exists(destPath))
                {
                    FileSystem.File.Delete(destPath);
                    logger.Debug($"Deleted {lib} from libs");
                }
            }

            foreach (var fileCopy in Mod.FileCopies)
            {
                if (FileSystem.File.Exists(fileCopy.Destination))
                {
                    logger.Debug($"Deleted {fileCopy.Destination}");
                    FileSystem.File.Delete(fileCopy.Destination);
                }
            }

            // Set ourself to uninstalled to avoid being included with potentially recursive dependency uninstalls
            _installed = false;

            // Uninstall mods depending on this mod (and collect in list)
            var uninstalledDependants = new List<QMod>();
            foreach (var mod in _provider.Mods.Values.Where(m => m.Installed && m.Mod.Dependencies.Any(d => d.Id == Id)))
            {
                logger.Warning($"Uninstalling dependant mod {mod.Id} v{mod.Version}: ");
                uninstalledDependants.Add(mod);
                await mod.UninstallAsyncInternal(depth + 1).ConfigureAwait(false);
            }

            // Uninstall libraries that no installed mods depend on
            foreach (var mod in _provider.Mods.Values.Where(m =>
                         m.Installed &&
                         m.Mod.IsLibrary &&
                         !_provider.Mods.Values.Any(dependingMod => dependingMod.Installed && dependingMod.Mod.Dependencies.Any(d => d.Id == m.Id))))
            {
                logger.Information($"Uninstalling library {mod.Id} v{mod.Version}: (as no installed mods depend on it)");
                await mod.UninstallAsyncInternal(depth + 1).ConfigureAwait(false);
            }
            
            logger.Information($"Uninstalled {Id} v{Version}. Removed {Mod.ModFileNames.Count} mod files, {Mod.LibraryFileNames.Count} lib files and {Mod.FileCopies.Count} file copies");

            _provider.InvokeModStatusChanged(this); // Now we actually forward the uninstall to the frontend
            return uninstalledDependants;
        }

        private async Task PrepareDependency(Dependency dependency, HashSet<string> installPath, ILogger logger)
        {
            // Cyclical dependency!
            if (installPath.Contains(dependency.Id))
            {
                return;
            }

            string prefix = $"-> {dependency.Id} {dependency.VersionRange}: ";

            var existing = _provider.Mods.Values.FirstOrDefault(m => m.Mod.Id == dependency.Id);
            if (existing != null)
            {
                if (dependency.VersionRange.IsSatisfied(existing.Mod.Version))
                {
                    // Dependency is already loaded and within the version range.
                    if (!existing.Installed)
                    {
                        logger.Information(prefix + $"Existing mod v{existing.Version} intersects range but is uninstalled, installing now:");
                        await existing.InstallAsyncInternal(installPath).ConfigureAwait(false); // Install it if it is not already installed
                    }
                    else
                    {
                        logger.Debug(prefix + $"Existing mod v{existing.Version} intersects range and is installed; no action required");
                    }
                    return;
                }
                // Otherwise we will need to upgrade the dependency
                if (dependency.DownloadIfMissing == null)
                {
                    throw new InstallationException(
                        $"Dependency {dependency.Id} is installed, but with an incorrect version ({existing.Mod.Version}) and does not specify a download link if missing, so the version couldn't be upgraded");
                }
                logger.Information(prefix + $"Existing mod v{existing.Version} does not intersect range. Downloading update . . .");
            }
            else if (dependency.DownloadIfMissing == null)
            {
                throw new InstallationException($"Dependency {dependency.Id} is not installed, and does not specify a download link if missing");
            }
            else
            {
                logger.Information(prefix + "No installation found. Downloading . . .");
            }

            try
            {
                using var resp = await HttpClient.GetAsync(dependency.DownloadIfMissing, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                await using var content = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);

                var memStream = new MemoryStream();
                await content.CopyToAsync(memStream);
                memStream.Position = 0;

                var loadedDep = (QMod) await _provider.ModManager.ImportMod(_provider, memStream, $"{dependency.Id}.qmod");

                // Quick sanity check to avoid people putting invalid download links and not noticing
                if (!dependency.VersionRange.IsSatisfied(loadedDep.Version))
                {
                    throw new InstallationException(
                        $"Dependency downloaded from {dependency.DownloadIfMissing} had version {loadedDep.Version}, which did not satisfy {dependency.VersionRange}");
                }

                // Actually install the dependency, which may involve installing more dependencies 
                logger.Information($"Now installing downloaded dependency (v{loadedDep.Version}):");
                await loadedDep.InstallAsyncInternal(installPath).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new InstallationException($"Failed to download dependency {dependency.Id} from {dependency.DownloadIfMissing}", ex);
            }
        }

        private async Task ExtractAsync(Stream stream, string destination)
        {
            // If files exist, we always overwrite them first
            if (FileSystem.File.Exists(destination)) FileSystem.File.Delete(destination);
            await using var destStream = FileSystem.File.OpenWrite(destination);

            await stream.CopyToAsync(destStream).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Mod.Dispose();
        }
    }
}
