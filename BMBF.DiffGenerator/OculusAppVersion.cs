using System.Text.Json.Serialization;

namespace BMBF.DiffGenerator;

public class OculusAppVersion
{
    public string ChangeLog { get; set; }

    public string RichChangeLog { get; set; }

    public string Version { get; set; }

    public int VersionCode { get; set; }

    public string Id { get; set; }

    [JsonConstructor]
    public OculusAppVersion(string changeLog, string richChangeLog, string version, int versionCode, string id)
    {
        ChangeLog = changeLog;
        RichChangeLog = richChangeLog;
        Version = version;
        VersionCode = versionCode;
        Id = id;
    }
}