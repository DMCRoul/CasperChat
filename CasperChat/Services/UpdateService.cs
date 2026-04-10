using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using CasperChat.Shared.Models;

namespace CasperChat.Client.Services
{
    public class UpdateService
    {
        private readonly HttpClient _httpClient;

        public UpdateService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<VersionCheckResult> CheckForUpdatesAsync()
        {
            VersionInfoResponse? serverInfo =
                await _httpClient.GetFromJsonAsync<VersionInfoResponse>("/version");

            if (serverInfo == null)
            {
                throw new InvalidOperationException("Сервер не вернул информацию о версии.");
            }

            Version currentVersion = GetCurrentVersion();
            Version latestVersion = ParseVersionOrDefault(serverInfo.LatestVersion);
            Version minimumSupportedVersion = ParseVersionOrDefault(serverInfo.MinimumSupportedVersion);

            bool updateAvailable = latestVersion > currentVersion;
            bool updateRequired = currentVersion < minimumSupportedVersion;

            return new VersionCheckResult
            {
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                MinimumSupportedVersion = minimumSupportedVersion,
                UpdateAvailable = updateAvailable,
                UpdateRequired = updateRequired,
                DownloadUrl = serverInfo.DownloadUrl ?? string.Empty,
                ReleaseNotes = serverInfo.ReleaseNotes ?? string.Empty
            };
        }

        private static Version GetCurrentVersion()
        {
            var version =
                Assembly.GetExecutingAssembly().GetName().Version
                ?? new Version(1, 0, 0, 0);

            return new Version(version.Major, version.Minor, version.Build < 0 ? 0 : version.Build);
        }

        private static Version ParseVersionOrDefault(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Version(1, 0, 0);

            return Version.TryParse(value, out var parsed)
                ? parsed
                : new Version(1, 0, 0);
        }
    }

    public class VersionCheckResult
    {
        public Version CurrentVersion { get; set; } = new Version(1, 0, 0);
        public Version LatestVersion { get; set; } = new Version(1, 0, 0);
        public Version MinimumSupportedVersion { get; set; } = new Version(1, 0, 0);
        public bool UpdateAvailable { get; set; }
        public bool UpdateRequired { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
    }
}