using System.Collections.Generic;
using BMBF.Resources;
using Newtonsoft.Json;

namespace BMBF.DiffGenerator;

public class Settings
{
    public string OutputDirectory { get; }
    
    public List<DiffInfo>? AdditionalDiffs { get; }
    
    [JsonConstructor]
    public Settings(string outputDirectory, List<DiffInfo>? additionalDiffs)
    {
        OutputDirectory = outputDirectory;
        AdditionalDiffs = additionalDiffs;
    }
}