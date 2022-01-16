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

        private readonly string _packageId;
        private bool _disposed;

        public bool CanAttemptImport(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() == ModExtension;
        }

        public QModProvider(string packageId, string modsPath, string libsPath, HttpClient httpClient, IFileSystem fileSystem, IModManager modManager)
        {
            _packageId = packageId;
            ModsPath = modsPath;
            LibsPath = libsPath;
            HttpClient = httpClient;
            FileSystem = fileSystem;
            ModManager = modManager;
        }

        public async Task<IMod?> TryParseModAsync(Stream stream)
        {
            ZipArchive? modArchive = null;
            QuestPatcher.QMod.QMod? qMod = null;
            try
            {
                try
                {
                    modArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                }
                catch (InvalidDataException)
                {
                    throw new InstallationException("Mod was not a valid ZIP archive");
                }
            
                try
                {
                    qMod = await QuestPatcher.QMod.QMod.ParseAsync(modArchive, false);
                
                    if (qMod.PackageId != _packageId)
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
            if (!(genericMod is QMod mod)) throw new ArgumentException("Cannot add non-qmod to qmod provider");
            
            await AddModAsyncInternal(mod, new HashSet<string>());
        }
        
        public async Task<bool> UnloadModAsync(IMod genericMod)
        {
            if (!(genericMod is QMod mod)) return false;
                
            if (mod.Installed)
            {
                await mod.UninstallAsyncInternal();
            }
            return UnloadModInternal(mod);
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
            mod.UpdateStatusInternal(); // Check whether or not the mod is installed
            
            // If an existing mod exists with this same ID, we will need to uninstall it
            // This may uninstall several dependant mods
            List<QMod> uninstalledDependants = new List<QMod>();
            if (Mods.TryGetValue(mod.Id, out var existing))
            {
                if (existing.Installed)
                {
                    uninstalledDependants = await existing.UninstallAsyncInternal();
                }

                UnloadModInternal(existing);
            }
            
            Mods.Add(mod.Id, mod);
            ModLoaded?.Invoke(mod.Id, mod);

            if (uninstalledDependants.Count > 0)
            {
                // Install the mod now, so that we can reinstall dependencies
                await mod.InstallAsyncInternal(installPath); 
                
                // Now reinstall the dependant mods
                foreach (QMod uninstalledDependant in uninstalledDependants)
                {
                    // Cannot reinstall dependant mod - version range is not satisfied
                    if (!uninstalledDependant.Mod.Dependencies.Single(d => d.Id == mod.Id).VersionRange
                            .IsSatisfied(mod.Version))
                    {
                        continue;
                    }
                    
                    await uninstalledDependant.InstallAsyncInternal(installPath);
                }
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