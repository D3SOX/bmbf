﻿using System;
using Newtonsoft.Json;
using Version = SemanticVersioning.Version;

namespace BMBF.Resources
{
    public class CoreMod
    {
        public string Id { get; set; }
        
        public Version Version { get; set; }
        
        public Uri DownloadLink { get; set; }
        
        public string? FileName { get; set; }
        
        [JsonConstructor]
        public CoreMod(string id, Version version, Uri downloadLink, string? fileName)
        {
            Id = id;
            Version = version;
            DownloadLink = downloadLink;
            FileName = fileName;
        }
    }
}