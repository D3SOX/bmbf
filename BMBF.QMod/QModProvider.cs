using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;
using Serilog;

namespace BMBF.QMod
{
    public class QModProvider : IModProvider
    {
        public const string ModExtension = ".qmod";

        public event EventHandler<IMod>? ModLoaded;

        public event EventHandler<string>? ModUnloaded;

        public event EventHandler<IMod>? ModStatusChanged;

        internal Dictionary<string, QMod> Mods { get; } = new Dictionary<string, QMod>();

        internal string ModsPath { get; }
        internal string LibsPath { get; }
        internal HttpClient HttpClient { get; }
        internal IFileSystem FileSystem { get; }
        internal IModManager ModManager { get; }
        internal ILogger Logger { get; }

        private readonly string _packageId;
        private bool _disposed;

        public bool CanAttemptImport(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() == ModExtension;
        }

        public QModProvider(string packageId, string modsPath, string libsPath, HttpClient httpClient, IFileSystem fileSystem, IModManager modManager, ILogger logger)
        {
            _packageId = packageId;
            ModsPath = modsPath;
            LibsPath = libsPath;
            HttpClient = httpClient;
            FileSystem = fileSystem;
            ModManager = modManager;
            Logger = logger;
        }

        public async Task<IMod?> TryParseModAsync(Stream stream, bool leaveOpen = false)
        {
            ZipArchive? modArchive = null;
            QuestPatcher.QMod.QMod? qMod = null;
            try
            {
                try
                {
                    modArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen);
                }
                catch (InvalidDataException)
                {
                    throw new InstallationException("Mod was not a valid ZIP archive");
                }

                try
                {
                    qMod = await QuestPatcher.QMod.QMod.ParseAsync(modArchive, false);

                    if (qMod.PackageId != null && qMod.PackageId != _packageId)
                    {
                        throw new InstallationException($"Incorrect package ID {qMod.PackageId}");
                    }

                    return new QMod(qMod, this);
                }
                catch (InvalidModException ex)
                {
                    throw new InstallationException(ex.Message);
                }
            }
            catch (Exception)
            {
                // If loading the mod failed, then we need to make sure to dispose the underlying ZipArchive and QMod
                modArchive?.Dispose();
                if (qMod != null) await qMod.DisposeAsync();
                throw;
            }
        }

        public async Task AddModAsync(IMod genericMod)
        {
            if (!(genericMod is QMod mod))
            {
                throw new ArgumentException("Cannot add non-qmod to qmod provider");
            }

            await AddModAsyncInternal(mod, new HashSet<string>());
        }

        public async Task<bool> UnloadModAsync(IMod genericMod)
        {
            if (!(genericMod is QMod mod))
            {
                return false;
            }

            if (mod.Installed)
            {
                Log.Information($"Uninstalling and unloading {genericMod.Id}");
                await mod.UninstallAsyncInternal();
            }
            else
            {
                Log.Information($"Unloading {genericMod.Id}");
            }
            return UnloadModInternal(mod);
        }

        public void UpdateModStatuses()
        {
            foreach (var mod in Mods.Values)
            {
                // Enable notifications for this status update
                mod.UpdateStatusInternal(true);
            }
        }

        internal void InvokeModStatusChanged(QMod mod)
        {
            ModStatusChanged?.Invoke(this, mod);
        }

        private bool UnloadModInternal(QMod mod)
        {
            if (Mods.Remove(mod.Id, out var removed))
            {
                removed.Dispose();
                ModUnloaded?.Invoke(this, removed.Id);
                return true;
            }
            return false;
        }

        internal async Task AddModAsyncInternal(QMod mod, HashSet<string> installPath)
        {
            // If a mod exists with this same ID, we will need to uninstall it and unload/delete it.
            bool needImmediateInstall = false;
            if (Mods.TryGetValue(mod.Id, out var existing))
            {
                if (existing.Installed)
                {
                    Logger.Information($"Uninstalling {existing.Id} v{existing.Version} to upgrade it to v{mod.Version}");
                    // We use UninstallSelfUnsafe here to avoid having to uninstall then reinstall the dependants
                    existing.UninstallSelfUnsafe();

                    // Now we have to uninstall any dependant mods that will not be compatible with a newer version of the mod
                    foreach (var m in Mods.Values)
                    {
                        if (!m.Installed)
                        {
                            continue;
                        }
                        var dep = m.Mod.Dependencies.FirstOrDefault(dep => dep.Id == mod.Id);
                        if (dep == null)
                        {
                            continue;
                        }

                        if (dep.VersionRange.IsSatisfied(mod.Version))
                        {
                            Logger.Debug($"Dependant {m.Id} depends on range {dep.VersionRange}, which is satisfied by new version {mod.Version}");
                            needImmediateInstall = true;
                        }
                        else
                        {
                            Logger.Warning($"Uninstalling incompatible dependant: {m.Id} depends on range {dep.VersionRange}, which is NOT satisfied by new version {mod.Version}");
                            await m.UninstallAsyncInternal();
                        }
                    }
                }


                UnloadModInternal(existing);
            }

            // Check whether or not the mod is installed
            // We deliberately do NOT notify changes yet, as this is only setting the initial mod status, which is not
            // actually a status change
            mod.UpdateStatusInternal(false);

            Mods.Add(mod.Id, mod);
            ModLoaded?.Invoke(mod.Id, mod);

            if (needImmediateInstall)
            {
                Logger.Information($"Installing {mod.Id} v{mod.Version} immediately, as it has installed dependants");
                await mod.InstallAsyncInternal(new HashSet<string>());
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (QMod mod in Mods.Values)
            {
                mod.Dispose();
            }
        }
    }
}
