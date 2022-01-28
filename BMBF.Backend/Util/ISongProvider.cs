using System.IO;

namespace BMBF.Backend.Util;

/// <summary>
/// Small abstraction over the files in a folder, used for loading songs either from a
/// <see cref="System.IO.Compression.ZipArchive"/> or from a physical directory.
/// </summary>
public interface ISongProvider
{
    /// <summary>
    /// Finds if a file with the given name exists in the folder
    /// </summary>
    /// <param name="name">File name</param>
    /// <returns>True if, and only if, a file with the given name exists</returns>
    bool Exists(string name);

    /// <summary>
    /// Opens the file with the given name
    /// </summary>
    /// <param name="name">The name of the file to open</param>
    /// <returns>A stream which can be used to read from the file</returns>
    /// <exception cref="FileNotFoundException">If no file exists with the given name</exception>
    Stream Open(string name);
}