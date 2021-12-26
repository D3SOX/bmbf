#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Android.Content.Res;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace BMBF
{
    /// <summary>
    /// Used to allow ASP.NET core to load configs from assets
    /// </summary>
    public class AssetFileProvider : IFileProvider
    {
        private class FileInfo : IFileInfo
        {

            private readonly string _path;
            private readonly AssetManager _assetManager;

            public FileInfo(string path, AssetManager assetManager)
            {
                _path = path;
                _assetManager = assetManager;
            }
            
            public Stream CreateReadStream()
            {
                return _assetManager.Open(_path) ?? throw new InvalidOperationException();
            }

            public bool Exists
            {
                get
                {
                    // TODO: Find if there is a better way of doing this, opening a stream every time will not be fast
                    using Stream? stream = _assetManager.Open(_path);
                    return stream != null;
                }
            }
            /// <summary>
            /// Since our asset files are compressed, we can't use OpenFd to get a file descriptor that would tell us the length
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            public long Length => throw new NotImplementedException();
            public string PhysicalPath => _path;
            public string Name => Path.GetFileName(_path);
            public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
            public bool IsDirectory => _path.EndsWith("/");
        }
        
        private class DirectoryContents : IDirectoryContents
        {
            private readonly string[]? _contents;
            private readonly AssetManager _assetManager;
            public DirectoryContents(string[]? contents, AssetManager assetManager)
            {
                _contents = contents;
                _assetManager = assetManager;
            }

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                if (_contents == null)
                {
                    throw new InvalidOperationException();
                }
                
                foreach(string path in _contents)
                {
                    yield return new FileInfo(path, _assetManager);
                }
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Exists => _contents != null;
        }

        private readonly AssetManager _assetManager;
        
        public AssetFileProvider(AssetManager assetManager)
        {
            _assetManager = assetManager;
        }
        
        public IFileInfo GetFileInfo(string subpath)
        {
            return new FileInfo(subpath, _assetManager);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return new DirectoryContents(_assetManager.List(subpath), _assetManager);
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}