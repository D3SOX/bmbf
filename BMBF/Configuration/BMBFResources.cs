using System;

#nullable disable

namespace BMBF.Configuration;

// ReSharper disable once InconsistentNaming
public class BMBFResources
{
    public const string Position = "BMBFResources";

    public Uri CoreModsIndex { get; set; }
        
    public Uri ModLoaderVersion { get; set; }
        
    public Uri LibUnityIndex { get; set; }
        
    public string LibUnityVersionTemplate { get; set; }
        
    public Uri DeltaIndex { get; set; }
        
    public string DeltaVersionTemplate { get; set; }
        
    public Uri ExtensionsIndex { get; set; }
}