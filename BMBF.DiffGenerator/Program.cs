using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Octodiff.Core;
using Octodiff.Diagnostics;
using Version = SemanticVersioning.Version;

namespace BMBF.DiffGenerator;

internal static class Program
{
    private class DummyProgressReporter : IProgressReporter
    {
        public void ReportProgress(string operation, long currentPosition, long total) { }
    }

    private static readonly JsonSerializer JsonSerializer = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented
    };

    private static async Task<SortedDictionary<Version, string>> DownloadVersions(Settings settings, List<OculusAppVersion> versions, string outputPath)
    {
        Directory.CreateDirectory(outputPath);
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", settings.Cookie);

        var result = new SortedDictionary<Version, string>();
        foreach (var version in versions)
        {
            string savePath = Path.Combine(outputPath, version.Version + ".apk");
            result[Version.Parse(version.Version)] = savePath;
            if (File.Exists(savePath))
            {
                Console.WriteLine($"{version.Version} is already saved");
                continue;
            }
            Console.WriteLine("Saving version " + version.Version + "...");

            using var saveFile = File.OpenWrite(savePath);
            using var resp = await client.GetAsync($"https://securecdn.oculus.com/binaries/download/?id={version.Id}");
            resp.EnsureSuccessStatusCode();
            await resp.Content.CopyToAsync(saveFile);
        }

        return result;
    }

    private static List<DiffInfo> GetDiffs(Settings settings, SortedDictionary<Version, string> versions)
    {
        // Add additional diffs
        // These are useful for adding a specific diff from say, the latest version to the latest moddable version
        // This avoids having to patch downgrade APK multiple times before being able to patch
        var diffs = settings.AdditionalDiffs ?? new();

        Version? last = null;
        foreach(var pair in versions)
        {
            if (last != null)
            {
                diffs.Add(new DiffInfo(pair.Key.ToString(), last.ToString(), null));
            }

            last = pair.Key;
        }

        return diffs;
    }

    private static async Task GenerateDiffs(SortedDictionary<Version, string> versions, List<DiffInfo> diffs, string indexPath, string outputPath)
    {
        Directory.CreateDirectory(outputPath);
        
        var signatureBuilder = new SignatureBuilder();
        var deltaBuilder = new DeltaBuilder();

        foreach (var pair in versions)
        {
            Stream? signatureStream = null;
            foreach (var diff in diffs.Where(diff => diff.FromVersion == pair.Key.ToString()))
            {
                diff.Name = $"{diff.FromVersion}-to-{diff.ToVersion}.delta";
                string diffPath = Path.Combine(outputPath, diff.Name);
                if (File.Exists(diffPath))
                {
                    Console.WriteLine($"Delta {diff.Name} already exists");
                    continue;
                }
                
                // Generate the file signature if not already done
                if (signatureStream is null)
                {
                    Console.WriteLine($"Generating signature for {pair.Key}");
                    signatureStream = new MemoryStream();
                    await using var basisStream = File.OpenRead(pair.Value);
                    signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
                }
                
                // Now we can generate the diff
                Console.WriteLine($"Generating delta {diff.Name}");
                await using var newFileStream = File.OpenRead(versions[Version.Parse(diff.ToVersion)]);
                await using var deltaStream = File.OpenWrite(diffPath);
                signatureStream.Position = 0;
                deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureStream, new DummyProgressReporter()), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
            }
        }

        Console.WriteLine("Saving index");
        await using var indexStream = File.OpenWrite(indexPath);
        using var writer = new StreamWriter(indexStream);
        using var jsonWriter = new JsonTextWriter(writer);
        JsonSerializer.Serialize(jsonWriter, diffs);
    }
    
    
    public static async Task Main(string[] args)
    {
        using var settingsReader = new StreamReader("settings.json");
        using var jsonReader = new JsonTextReader(settingsReader);
        var settings = JsonSerializer.Deserialize<Settings>(jsonReader);
        if (settings == null)
        {
            await Console.Error.WriteLineAsync("Settings were null!");
            return;
        }
        
        string apksDir = Path.Combine(settings.OutputDirectory, "APKs");
        string diffsPath = Path.Combine(settings.OutputDirectory, "Diffs");
        string diffIndexPath = Path.Combine(settings.OutputDirectory, "diffIndex.json");
        
        var oculusVersions = await new AppVersionFinder(settings.AccessToken).GetAppVersions(2448060205267927);
        var versionPaths = await DownloadVersions(settings, oculusVersions, apksDir);

        var diffs = GetDiffs(settings, versionPaths);
        await GenerateDiffs(versionPaths, diffs, diffIndexPath, diffsPath);
    }
}