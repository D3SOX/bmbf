namespace BMBF.Models.Setup
{
    /// <summary>
    /// Represents the current point in setup BMBF is at
    /// </summary>
    public class SetupStatus
    {
        /// <summary>
        /// Status of downgrading if the APK is currently being downgraded
        /// </summary>
        public DowngradingStatus? DowngradingStatus { get; set; }

        public SetupStage Stage { get; set; } = SetupStage.Downgrading;
        
        public bool IsInProgress { get; set; }
        
        /// <summary>
        /// Currently downgraded to Beat Saber version
        /// </summary>
        public string CurrentBeatSaberVersion { get; set; }
        
        public SetupStatus(string currentBeatSaberVersion)
        {
            CurrentBeatSaberVersion = currentBeatSaberVersion;
        }
    }
}