using FlightRecorder.Client.SimConnectMSFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace FlightRecorder.Client.Logics;

public class SavedFlightItem
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("aircraftTitle")]
    public string AircraftTitle { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public long StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public long EndTime { get; set; }

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("frameCount")]
    public int FrameCount { get; set; }

    [JsonPropertyName("profileId")]
    public string? ProfileId { get; set; }

    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public static class SavedFlightsLogic
{
    public static string GetSavedFlightsDirectory()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var primaryFolder = Path.Combine(baseDir, "SavedFlights");
            if (!Directory.Exists(primaryFolder))
            {
                Directory.CreateDirectory(primaryFolder);
            }

            // Test if writable
            var testFile = Path.Combine(primaryFolder, ".test_write");
            File.WriteAllText(testFile, "ok");
            File.Delete(testFile);

            return primaryFolder;
        }
        catch
        {
            // Fallback for non-admin environments
            var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder", "SavedFlights");
            if (!Directory.Exists(fallback))
            {
                Directory.CreateDirectory(fallback);
            }
            return fallback;
        }
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var result = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(result) ? "Flight" : result;
    }

    public static async Task SaveMetadataAsync(string flightFilePath, SavedData data)
    {
        try
        {
            var metaPath = flightFilePath + ".meta.json";
            var fileInfo = new FileInfo(flightFilePath);
            var duration = (data.Records.Count > 1) 
                ? (data.Records.Last().Time - data.Records.First().Time) / 1000.0 
                : 0.0;

            var meta = new SavedFlightItem
            {
                FilePath = flightFilePath,
                FileName = Path.GetFileName(flightFilePath),
                AircraftTitle = data.StartState?.AircraftTitle ?? "Flight",
                StartTime = data.StartTime,
                EndTime = data.EndTime,
                DurationSeconds = duration,
                FrameCount = data.Records.Count,
                ProfileId = data.ProfileId,
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                CreatedAt = fileInfo.Exists ? fileInfo.CreationTime : DateTime.Now
            };

            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metaPath, json);
        }
        catch
        {
            // Non-critical if metadata writing fails
        }
    }

    public static List<SavedFlightItem> GetSavedFlights(IStorageLogic storageLogic)
    {
        var dir = GetSavedFlightsDirectory();
        var results = new List<SavedFlightItem>();

        if (!Directory.Exists(dir)) return results;

        var files = Directory.GetFiles(dir, "*.*")
            .Where(f => f.EndsWith(".fltrec", StringComparison.OrdinalIgnoreCase) || 
                        f.EndsWith(".flightrecorder", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var metaPath = file + ".meta.json";
                if (File.Exists(metaPath))
                {
                    var metaJson = File.ReadAllText(metaPath);
                    var item = JsonSerializer.Deserialize<SavedFlightItem>(metaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (item != null)
                    {
                        item.FilePath = file;
                        item.FileName = Path.GetFileName(file);
                        results.Add(item);
                        continue;
                    }
                }

                // If no companion metadata, parse file lazily
                var fi = new FileInfo(file);
                using var fs = File.OpenRead(file);
                var data = storageLogic.LoadAsync(fs).GetAwaiter().GetResult();
                if (data != null)
                {
                    var duration = (data.Records.Count > 1) 
                        ? (data.Records.Last().Time - data.Records.First().Time) / 1000.0 
                        : 0.0;

                    var item = new SavedFlightItem
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        AircraftTitle = data.StartState?.AircraftTitle ?? "Flight",
                        StartTime = data.StartTime,
                        EndTime = data.EndTime,
                        DurationSeconds = duration,
                        FrameCount = data.Records.Count,
                        ProfileId = data.ProfileId,
                        FileSizeBytes = fi.Length,
                        CreatedAt = fi.CreationTime
                    };

                    SaveMetadataAsync(file, data).GetAwaiter().GetResult();
                    results.Add(item);
                }
            }
            catch
            {
                // Skip unreadable files
            }
        }

        return results;
    }

    public static bool DeleteSavedFlight(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            var meta = filePath + ".meta.json";
            if (File.Exists(meta)) File.Delete(meta);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
