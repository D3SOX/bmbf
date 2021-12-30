using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BMBF.Resources
{
    public class CoreMods
    {
        public DateTime LastUpdated { get; set; }

        public List<CoreMod> Mods { get; set; }
        
        [JsonConstructor]
        public CoreMods(DateTime lastUpdated, List<CoreMod> mods)
        {
            LastUpdated = lastUpdated;
            Mods = mods;
        }
    }
}