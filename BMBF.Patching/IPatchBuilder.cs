using System.Threading;
using System.Threading.Tasks;
using SemanticVersioning;
using Serilog;

namespace BMBF.Patching;

/// <summary>
/// Abstraction over a tool for patching the APK with convenient builder syntax.
/// </summary>
public interface IPatchBuilder
{
    /// <summary>
    /// Specifies the modloader type to save in the tag
    /// </summary>
    /// <param name="modloaderName">Name of the modloader</param>
    /// <param name="modloaderVersion">Semver of the modloader</param>
    IPatchBuilder WithModloader(string modloaderName, Version modloaderVersion);

    /// <summary>
    /// Adds a file to patch
    /// </summary>
    /// <param name="apkFilePath">Path of the file within the APK</param>
    /// <param name="patchFileDelegate">Delegate used to modify the file's contents</param>
    IPatchBuilder PatchFile(string apkFilePath, PatchFileDelegate patchFileDelegate);

    /// <summary>
    /// Adds a file to be added/replaced in the APK
    /// </summary>
    /// <param name="apkFilePath">Path of the file within the APK</param>
    /// <param name="overwriteMode">Settings for file overwriting</param>
    /// <param name="getFileDelegate">Delegate used to open the source file</param>
    IPatchBuilder ModifyFile(string apkFilePath, OverwriteMode overwriteMode, GetFileDelegate getFileDelegate);

    /// <summary>
    /// Adds a file to be added/replaced in the APK
    /// </summary>
    /// <param name="apkFilePath">Path of the file within the APK</param>
    /// <param name="overwriteMode">Settings for file overwriting</param>
    /// <param name="getFileDelegate">Delegate used to fetch the source file asynchronously</param>
    IPatchBuilder ModifyFileAsync(string apkFilePath, OverwriteMode overwriteMode, GetFileAsyncDelegate getFileDelegate);

    /// <summary>
    /// Disables adding the modded tag to this APK
    /// </summary>
    IPatchBuilder DisableTagging();

    /// <summary>
    /// Sets whether or not patching will fail if a modded tag already exists within the APK
    /// </summary>
    /// <param name="allowExistingTag">If true, patching will not fail if a tag already exists</param>
    IPatchBuilder SetAllowExistingTag(bool allowExistingTag);

    /// <summary>
    /// Sets the certificate to sign the APK with after patching
    /// </summary>
    /// <param name="certificate">Certificate and private key, in PEM format</param>
    IPatchBuilder Sign(string certificate);

    /// <summary>
    /// Patches the APK with the given path with the options in this builder
    /// </summary>
    /// <param name="apkPath">Path of the APK to patch</param>
    /// <param name="logger">Logger to print information to during patching</param>
    /// <param name="ct">Token to cancel patching</param>
    Task Patch(string apkPath, ILogger logger, CancellationToken ct);
}
