using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMBF.Patching;

/// <summary>
/// Asynchronously fetches a file to be used during patching.
/// <param name="ct">Cancellation token which is canceled if patching is canceled</param>
/// <returns>The stream used to write the file to the APK, or null if the file should be skipped</returns>
/// </summary>
public delegate Task<Stream?> GetFileAsyncDelegate(CancellationToken ct);
