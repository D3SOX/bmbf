using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;

namespace BMBF.QMod
{
    public class QModProvider : IModProvider
    {
        public const string ModExtension = ".qmod";
        
        public event EventHandler<ModLoadedEventArgs>? ModLoaded;

        public event EventHandler<IMod>? ModUnloaded;
        
        public event EventHandler<IMod>? ModStatusChanged;

        internal Dictionary<string, QMod> Mods { get; } = new Dictionary<string, QMod>();

        internal string ModsPath { get; }
        internal string LibsPath { get; }
        internal HttpClient HttpClient { get; }
        internal IFileSystem FileSystem { get; }
        internal SemaphoreSlim InstallLock { get; } = new SemaphoreSlim(1);

        private readonly string _packageId;
        private bool _disposed;

        public bool CanAttemptImport(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() == ModExtension;
        }

        public QModProvider(string packageId, string modsPath, string libsPath, HttpClient httpClient, IFileSystem fileSystem)
        {
            _packageId = packageId;
            ModsPath = modsPath;
            LibsPath = libsPath;
            HttpClient = httpClient;
            FileSystem = fileSystem;
        }

        public async ValueTask<IMod?> TryImportModAsync(Stream stream, string fileName)
        {
            if (Path.GetExtension(fileName).ToLower() != ModExtension)
            {
                // Can't process mods which aren't qmod files
                return null;
            }

            await InstallLock.WaitAsync();
            try
            {
                return await ImportModAsyncInternal(stream, new HashSet<string>());
            }
            finally
            {
                InstallLock.Release();
            }
        }
        
        public async Task<bool> UnloadModAsync(IMod genericMod)
        {
            await InstallLock.WaitAsync();
            try
            {
                if (!(genericMod is QMod mod)) return false;
                
                if (mod.Installed)
                {
                    await mod.UninstallAsyncInternal();
                }
                return UnloadModInternal(mod);
            }
            finally
            {
                InstallLock.Release();
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
                ModUnloaded?.Invoke(this, removed);
                return true;
            }
            return false;
        }

        internal async Task<QMod> ImportModAsyncInternal(Stream stream, HashSet<string> installPath)
        {
            ZipArchive modArchive;
            try
            {
                modArchive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            }
            catch (InvalidDataException)
            {
                throw new InstallationException("Mod was not a valid ZIP archive");
            }
            
            QuestPatcher.QMod.QMod qMod;
            try
            {
                qMod = await QuestPatcher.QMod.QMod.ParseAsync(modArchive, false);
            }
            catch (InvalidModException ex)
            {
                throw new InstallationException(ex.Message);
            }
            catch (InvalidDataException)
            {
                throw new InstallationException("Mod was not a valid ZIP archive");
            }
            
            // Make sure that the mod is for the correct app!
            if (qMod.PackageId != _packageId)
            {
                throw new InstallationException($"Mod was for package id {qMod.PackageId}, not {_packageId}");
            }

            var mod = new QMod(qMod, this);
            
            mod.UpdateStatusInternal(); // Check whether or not the mod is installed
            
            stream.Position = 0;

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
            ModLoaded?.Invoke(mod.Id, new ModLoadedEventArgs(mod, stream, $"{mod.Id}_v{mod.Version}.qmod"));

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

            return mod;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            foreach (QMod mod in Mods.Values)
            {
                mod.Dispose();
            }
            
            InstallLock.Dispose();
        }
    }
}