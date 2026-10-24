using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FlightRecorder.Client.Logics
{
    public static class VpilotService
    {
        public const string PipeName = "FlightRecorder_vPilot";
        private const string PluginFileName = "FlightRecorder.VpilotPlugin.dll";

        /// <summary>
        /// Automatically locates vPilot on the user's system and installs or updates the plugin DLL silently.
        /// If vPilot is not installed, this method silently exits without any prompts.
        /// </summary>
        public static void EnsurePluginInstalled(ILogger? logger = null)
        {
            try
            {
                var vpilotDir = FindVpilotDirectory();
                if (string.IsNullOrEmpty(vpilotDir) || !Directory.Exists(vpilotDir))
                {
                    logger?.LogDebug("[vPilot] vPilot installation directory not found on system.");
                    return;
                }

                var vpilotExe = Path.Combine(vpilotDir, "vPilot.exe");
                if (!File.Exists(vpilotExe))
                {
                    logger?.LogDebug("[vPilot] vPilot.exe not found in {Dir}.", vpilotDir);
                    return;
                }

                var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginFileName);
                if (!File.Exists(sourcePath))
                {
                    logger?.LogWarning("[vPilot] Source plugin DLL not found at {SourcePath}.", sourcePath);
                    return;
                }

                var pluginsDir = Path.Combine(vpilotDir, "Plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    Directory.CreateDirectory(pluginsDir);
                }

                var targetPath = Path.Combine(pluginsDir, PluginFileName);
                bool needsCopy = false;

                if (!File.Exists(targetPath))
                {
                    needsCopy = true;
                }
                else
                {
                    var sourceTime = File.GetLastWriteTimeUtc(sourcePath);
                    var targetTime = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceTime > targetTime)
                    {
                        needsCopy = true;
                    }
                }

                if (needsCopy)
                {
                    File.Copy(sourcePath, targetPath, overwrite: true);
                    logger?.LogInformation("[vPilot] Plugin automatically installed/updated at {TargetPath}.", targetPath);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "[vPilot] Failed to automatically copy plugin to vPilot directory.");
            }
        }

        /// <summary>
        /// Attempts to connect to vPilot via Named Pipe and request disconnection from the VATSIM network.
        /// Returns true if vPilot was connected and was successfully requested to disconnect.
        /// </summary>
        public static async Task<bool> DisconnectIfConnectedAsync(ILogger? logger = null)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                
                using var ctsConnect = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
                await pipeClient.ConnectAsync(ctsConnect.Token).ConfigureAwait(false);

                using var reader = new StreamReader(pipeClient, Encoding.UTF8, false, 1024, leaveOpen: true);
                using var writer = new StreamWriter(pipeClient, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                await writer.WriteLineAsync("DISCONNECT").ConfigureAwait(false);

                using var ctsRead = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                
                var readTask = reader.ReadLineAsync();
                var completedTask = await Task.WhenAny(readTask, Task.Delay(1000, ctsRead.Token)).ConfigureAwait(false);

                if (completedTask == readTask)
                {
                    var response = (await readTask.ConfigureAwait(false))?.Trim();
                    logger?.LogInformation("[vPilot] Disconnect response received: {Response}", response);

                    if (string.Equals(response, "DISCONNECTED_OK", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout connecting to pipe or reading response (vPilot is likely not running or pipe busy)
                logger?.LogDebug("[vPilot] Named pipe connection timed out. vPilot is likely not running.");
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "[vPilot] Named pipe communication error.");
            }

            return false;
        }

        /// <summary>
        /// Detects the vPilot installation directory using Registry and default LocalAppData paths.
        /// </summary>
        public static string? FindVpilotDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // 1. Check HKCU Uninstall Registry
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\vPilot");
                    if (key != null)
                    {
                        var installLoc = key.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrWhiteSpace(installLoc) && Directory.Exists(installLoc))
                        {
                            return installLoc;
                        }

                        var uninstallStr = key.GetValue("UninstallString") as string;
                        if (!string.IsNullOrWhiteSpace(uninstallStr))
                        {
                            var dir = Path.GetDirectoryName(uninstallStr);
                            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                            {
                                return dir;
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Default standard path: %LOCALAPPDATA%\vPilot
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var standardPath = Path.Combine(localAppData, "vPilot");
                if (Directory.Exists(standardPath) && File.Exists(Path.Combine(standardPath, "vPilot.exe")))
                {
                    return standardPath;
                }
            }
            catch { }

            return null;
        }
    }
}
