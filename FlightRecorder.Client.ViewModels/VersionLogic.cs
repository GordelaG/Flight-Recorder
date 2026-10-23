using Microsoft.Extensions.Logging;

namespace FlightRecorder.Client
{
    public class VersionLogic
    {
        private readonly ILogger<VersionLogic> logger;

        public VersionLogic(ILogger<VersionLogic> logger)
        {
            this.logger = logger;
        }

        public string GetVersion()
        {
            try
            {
                var versionFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (System.IO.File.Exists(versionFile))
                {
                    var content = System.IO.File.ReadAllText(versionFile).Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        return content;
                    }
                }
            }
            catch { }

            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            if (version == null)
            {
                logger.LogWarning("Cannot get assembly version. Revert to 0.0.0.0.");
                return "0.0.0.0";
            }
            else
            {
                return version;
            }
        }
    }
}
