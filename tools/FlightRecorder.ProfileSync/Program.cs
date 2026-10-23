using System;
using System.IO;
using System.Threading.Tasks;
using FlightRecorder.Client.Logics;
using Microsoft.Extensions.Logging;

namespace FlightRecorder.ProfileSync;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=================================================");
        Console.WriteLine("  Flight Recorder - HubHop Profile Sync Tool");
        Console.WriteLine("=================================================");
        Console.ResetColor();

        string targetDir = Path.Combine(Directory.GetCurrentDirectory(), "FlightRecorder.Client", "aircraft_profiles");
        var outputIdx = Array.IndexOf(args, "--output");
        if (outputIdx >= 0 && outputIdx + 1 < args.Length)
        {
            targetDir = Path.GetFullPath(args[outputIdx + 1]);
        }

        Console.WriteLine($"Target directory: {targetDir}");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<HubHopSyncService>();
        var profileManager = new AircraftProfileManager(customProfilesDirectory: targetDir);
        var syncService = new HubHopSyncService(profileManager, logger);

        var progress = new Progress<string>(msg =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[HubHop] {msg}");
            Console.ResetColor();
        });

        var result = await syncService.SyncFromApiAsync(progress, targetFolder: targetDir);

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[SUCCESS] {result.Message}");
            Console.WriteLine($"Total Presets: {result.TotalPresetsDownloaded:N0}");
            Console.WriteLine($"Profiles Written: {result.ProfilesGenerated}");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] {result.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}
