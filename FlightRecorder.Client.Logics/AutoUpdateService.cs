using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FlightRecorder.Client.Logics
{
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
    }

    public class AutoUpdateService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<AutoUpdateService>? logger;

        public AutoUpdateService(HttpClient? httpClient = null, ILogger<AutoUpdateService>? logger = null)
        {
            this.httpClient = httpClient ?? new HttpClient();
            this.logger = logger;

            if (this.httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                this.httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("FlightRecorder-AutoUpdater", "1.0"));
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(
            string currentVersionString,
            string repoOwner = "GordelaG",
            string repoName = "Flight-Recorder",
            CancellationToken cancellationToken = default)
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = currentVersionString
            };

            try
            {
                var apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger?.LogWarning("GitHub release check returned status {StatusCode}", response.StatusCode);
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : string.Empty;
                var releaseName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : string.Empty;
                var releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : string.Empty;

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    return result;
                }

                result.LatestVersion = tagName;
                result.ReleaseName = releaseName ?? tagName;
                result.ReleaseNotes = releaseNotes ?? string.Empty;

                // Look for .exe asset in the release (FlightRecorder-Setup.exe preferred)
                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var assetName = asset.TryGetProperty("name", out var aName) ? aName.GetString() : string.Empty;
                        var downloadUrl = asset.TryGetProperty("browser_download_url", out var dUrl) ? dUrl.GetString() : string.Empty;

                        if (!string.IsNullOrWhiteSpace(assetName) && !string.IsNullOrWhiteSpace(downloadUrl))
                        {
                            if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                result.DownloadUrl = downloadUrl;
                                if (assetName.Contains("Setup", StringComparison.OrdinalIgnoreCase) ||
                                    assetName.Contains("Installer", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Preferred installer found
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(result.DownloadUrl) && IsNewerVersion(currentVersionString, tagName))
                {
                    if (HasAlreadyAttemptedVersion(tagName))
                    {
                        logger?.LogInformation("Update {Remote} was already installed or attempted previously. Skipping auto-update to prevent loop.", tagName);
                        result.IsUpdateAvailable = false;
                    }
                    else
                    {
                        result.IsUpdateAvailable = true;
                        logger?.LogInformation("New version detected: {Remote} (Current: {Current})", tagName, currentVersionString);
                    }
                }
                else
                {
                    logger?.LogInformation("Application is up to date: Current={Current}, Remote={Remote}", currentVersionString, tagName);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to check for updates from GitHub releases");
            }

            return result;
        }

        public async Task<string> DownloadInstallerAsync(
            string downloadUrl,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"FlightRecorder-Setup-{Guid.NewGuid():N}.exe");
            logger?.LogInformation("Downloading installer from {Url} to {Path}", downloadUrl, tempPath);

            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    var percentage = (int)((totalRead * 100) / totalBytes);
                    progress.Report(percentage);
                }
            }

            logger?.LogInformation("Installer download completed ({Bytes} bytes).", totalRead);
            return tempPath;
        }

        public void ApplyUpdateAndRestart(string installerPath)
        {
            if (!File.Exists(installerPath))
            {
                throw new FileNotFoundException("Installer file not found", installerPath);
            }

            logger?.LogInformation("Launching installer {Path} with silent auto-restart parameters...", installerPath);

            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        public bool IsDevelopmentEnvironment()
        {
            try
            {
                if (Debugger.IsAttached) return true;

                var baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant();
                if (baseDir.Contains(@"\bin\debug") || baseDir.Contains(@"\bin\release") ||
                    baseDir.Contains(@"/bin/debug") || baseDir.Contains(@"/bin/release"))
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool HasAlreadyAttemptedVersion(string versionTag)
        {
            try
            {
                var stateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder", "last_update_attempt.txt");
                if (File.Exists(stateFile))
                {
                    var recorded = File.ReadAllText(stateFile).Trim();
                    if (string.Equals(recorded, versionTag, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public void RecordUpdateAttempt(string versionTag)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder");
                Directory.CreateDirectory(folder);
                var stateFile = Path.Combine(folder, "last_update_attempt.txt");
                File.WriteAllText(stateFile, versionTag);
            }
            catch { }
        }

        public static bool IsNewerVersion(string currentVersionStr, string remoteTag)
        {
            if (string.IsNullOrWhiteSpace(remoteTag)) return false;

            var cleanCurrent = CleanVersionString(currentVersionStr);
            var cleanRemote = CleanVersionString(remoteTag);

            if (Version.TryParse(cleanCurrent, out var cur) && Version.TryParse(cleanRemote, out var rem))
            {
                var normCur = NormalizeVersion(cur);
                var normRem = NormalizeVersion(rem);
                return normRem > normCur;
            }

            return false;
        }

        public static string CleanVersionString(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.0.0.0";
            var v = version.Trim();
            if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                v = v.Substring(1);
            }

            var dashIdx = v.IndexOf('-');
            if (dashIdx > 0)
            {
                v = v.Substring(0, dashIdx);
            }

            return v;
        }

        private static Version NormalizeVersion(Version v)
        {
            return new Version(
                Math.Max(0, v.Major),
                Math.Max(0, v.Minor),
                Math.Max(0, v.Build),
                Math.Max(0, v.Revision)
            );
        }
    }
}
