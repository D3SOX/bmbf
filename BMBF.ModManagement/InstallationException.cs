using System;

namespace BMBF.ModManagement
{
    /// <summary>
    /// Represents an error which occurred while installing, uninstalling, or loading a mod.
    /// </summary>
    public class InstallationException : Exception
    {
        public InstallationException(string message) : base(message) { }

        public InstallationException(string message, Exception cause) : base(message, cause) { }
    }
}