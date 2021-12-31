using System.IO;

namespace BMBF.Patching
{
    public delegate void PatchFileDelegate(Stream readFrom, Stream writeTo);
}