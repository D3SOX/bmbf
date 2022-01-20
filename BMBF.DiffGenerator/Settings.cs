using System.Collections.Generic;
using System.Text.Json.Serialization;
using BMBF.Resources;

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