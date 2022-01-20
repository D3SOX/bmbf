using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BMBF.Resources
{
    public class CoreMods
    {
        public string LastUpdated { get; set; }

        public List<CoreMod> Mods { get; set; }
        
        [JsonConstructor]
        public CoreMods(string lastUpdated, List<CoreMod> mods)
        {
            LastUpdated = lastUpdated;
            Mods = mods;
        }
    }
}