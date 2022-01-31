using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Content.Res;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace BMBF;

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
                var files = _assetManager.List(Path.GetDirectoryName(_path) ?? "");
                if (files == null) return false;

                var exists = files.Contains(Path.GetFileName(_path));
                return exists;
            }
        }

        /// <summary>
        /// Since our asset files are compressed, we can't use OpenFd to get a file descriptor that would tell us the length
        /// </summary>
        public long Length => 0;
        public string? PhysicalPath => null;
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

            foreach (string path in _contents)
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
    private readonly string _basePath;

    public AssetFileProvider(AssetManager assetManager, string basePath = "")
    {
        _assetManager = assetManager;
        _basePath = basePath;
    }

    private string GetAssetPath(string path)
    {
        var nonRootedPath = Path.IsPathRooted(path) ? path[1..] : path;
        return Path.Combine(_basePath, nonRootedPath);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return new FileInfo(GetAssetPath(subpath), _assetManager);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new DirectoryContents(_assetManager.List(GetAssetPath(subpath)), _assetManager);
    }

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }
}