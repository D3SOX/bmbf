using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace BMBF.Patching;

/// <summary>
/// Represents a tool to sign an APK
/// </summary>
public interface IApkSigner
{
    /// <summary>
    /// Signs the APK with the given path with the given certificate
    /// </summary>
    /// <param name="fileSystem">File system that the APK is located on</param>
    /// <param name="path">Path to the APK to sign</param>
    /// <param name="pemData">PEM of the certificate and private key</param>
    /// <param name="signerName">Name of the signer in the manifest</param>
    /// <param name="ct">Token which can be used to cancel signing the APK</param>
    Task SignApkAsync(IFileSystem fileSystem, string path, string pemData, string signerName, CancellationToken ct);

    /// <summary>
    /// Creates a new X509 certificate and returns its data in PEM format.
    /// </summary>
    string GenerateNewCertificatePem();
}

