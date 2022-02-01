using System.IO;

namespace BMBF.Patching;

/// <summary>
/// Opens a file to be used during patching.
/// <returns>The stream used to write the file to the APK, or null if the file should be skipped</returns>
/// </summary>
public delegate Stream? GetFileDelegate();
