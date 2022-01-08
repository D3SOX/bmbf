using System.Collections.Generic;
using BMBF.Resources;
using Newtonsoft.Json;

namespace BMBF.DiffGenerator;

public class Settings
{
    public string OutputDirectory { get; }
    
    public string AccessToken { get; }
    
    public List<DiffInfo>? AdditionalDiffs { get; }
    
    [JsonConstructor]
    public Settings(string outputDirectory, string accessToken, List<DiffInfo>? additionalDiffs)
    {
        OutputDirectory = outputDirectory;
        AccessToken = accessToken;
        AdditionalDiffs = additionalDiffs;
    }
}