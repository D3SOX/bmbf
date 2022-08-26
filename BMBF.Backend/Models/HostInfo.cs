namespace BMBF.Backend.Models
{
    /// <summary>
    /// Information about where the requesting frontend/BMBF backend is running.
    /// </summary>
    public class HostInfo
    {
        /// <summary>
        /// The local IP address of the server on which the backend is running.
        /// </summary>
        public string HostLocalIp { get; set; }

        /// <summary>
        /// The address of the connecting frontend.
        /// </summary>
        public string ConnectingIp { get; set; }

        /// <summary>
        /// The BMBF version.
        /// </summary>
        public string Version { get; set; }

        public HostInfo(string hostLocalIp, string connectingIp, string version)
        {
            HostLocalIp = hostLocalIp;
            ConnectingIp = connectingIp;
            Version = version;
        }
    }
}
