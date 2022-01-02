using System.IO;

namespace BMBF.Util
{
    /// <summary>
    /// Small abstraction over the files in a folder
    /// </summary>
    public interface IFolderProvider
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
}