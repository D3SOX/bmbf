using System;
using System.IO;
using System.Threading.Tasks;

namespace BMBF.ModManagement
{
    /// <summary>
    /// Extensions for importing QMods during testing.
    /// </summary>
    public static class ModProviderExtensions
    {
        /// <summary>
        /// Parses and adds a mod to the given provider.
        /// </summary>
        /// <param name="provider">The provider to import the mod with</param>
        /// <param name="stream">The stream to import the mod from</param>
        /// <returns>The parsed and added mod</returns>
        /// <exception cref="FormatException">If the mod was not the correct type for this provider</exception>
        /// <exception cref="InstallationException">If registering the mod with the provider fails</exception>
        public static async Task<IMod> ParseAndAddMod(this IModProvider provider, Stream stream)
        {
            var mod = await provider.TryParseModAsync(stream) ?? throw new FormatException("The given file was not a valid mod");
            await provider.AddModAsync(mod);
            return mod;
        }
        
        /// <summary>
        /// Parses and adds a mod to the given provider.
        /// </summary>
        /// <param name="provider">The provider to import the mod with</param>
        /// <param name="stream">The stream to import the mod from</param>
        /// <param name="fileName">The </param>
        /// <returns>The parsed and added mod</returns>
        /// <exception cref="FormatException">If the mod was not the correct type for this provider</exception>
        /// <exception cref="InstallationException">If registering the mod with the provider fails</exception>
        public static async Task<IMod> ParseAndAddMod(this IModProvider provider, Stream stream, string fileName)
        {
            if (!provider.CanAttemptImport(fileName))
                throw new FormatException($"The given provider could not import a mod with name {fileName}");

            return await ParseAndAddMod(provider, stream);
        }
    }
}