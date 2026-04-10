namespace CasperChat.Shared.Models
{
    public class VersionInfoResponse
    {
        public string LatestVersion { get; set; } = "1.0.0";
        public string MinimumSupportedVersion { get; set; } = "1.0.0";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
    }
}