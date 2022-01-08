using System;

namespace BMBF
{
    public class ImportException : Exception
    {
        public ImportException(string? message) : base(message) { }

        public ImportException(string? message, Exception cause) : base(message, cause) { }
    }
}