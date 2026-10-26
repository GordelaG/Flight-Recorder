using FlightRecorder.Client.Logic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlightRecorder.Client.Logics;

public class FileSettingsLogic : ISettingsLogic
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string filePath;
    private readonly ILogger<FileSettingsLogic> logger;
    private static readonly SemaphoreSlim sm = new(1);
    private Settings? settings = null;

    public FileSettingsLogic(ILogger<FileSettingsLogic> logger) : this(logger, null)
    {
    }

    public FileSettingsLogic(ILogger<FileSettingsLogic> logger, string? customFilePath)
    {
        this.logger = logger;
        this.filePath = customFilePath ?? ResolveSettingsFilePath();
    }

    public static string ResolveSettingsFilePath()
    {
        var localAppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder");
        var appDataFile = Path.Combine(localAppDataDir, "settings.json");

        // Migration check: If appDataFile does not exist yet, check if there's an existing settings.json
        if (!File.Exists(appDataFile))
        {
            try
            {
                var baseDirFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(baseDirFile))
                {
                    Directory.CreateDirectory(localAppDataDir);
                    File.Copy(baseDirFile, appDataFile, true);
                }
                else if (File.Exists("settings.json"))
                {
                    Directory.CreateDirectory(localAppDataDir);
                    File.Copy("settings.json", appDataFile, true);
                }
            }
            catch
            {
                // Silently ignore migration exceptions and proceed to use appDataFile
            }
        }

        return appDataFile;
    }

    public async Task<bool> IsShortcutKeysEnabledAsync()
    {
        var s = await LoadAsync();
        return s.ShortcutKeysEnabled;
    }

    public Task SetShortcutKeysEnabledAsync(bool value)
    {
        return SaveAsync(s => s.ShortcutKeysEnabled = value);
    }

    public async Task<Dictionary<Shortcuts, ShortcutKey>?> GetShortcutKeysAsync()
    {
        var s = await LoadAsync();
        return s.ShortcutKeys;
    }

    public Task SetShortcutKeysAsync(Dictionary<Shortcuts, ShortcutKey> shortcutKeys)
    {
        return SaveAsync(s => s.ShortcutKeys = shortcutKeys);
    }

    public async Task<string?> GetDefaultSaveFolderAsync()
    {
        var s = await LoadAsync();
        return s.DefaultSaveFolder;
    }

    public Task SetDefaultSaveFolderAsync(string? folderPath)
    {
        return SaveAsync(s => s.DefaultSaveFolder = folderPath);
    }

    public async Task<bool> GetHudAutoHideAsync()
    {
        var s = await LoadAsync();
        return s.HudAutoHide;
    }

    public Task SetHudAutoHideAsync(bool value)
    {
        return SaveAsync(s => s.HudAutoHide = value);
    }

    public async Task<bool> GetHudTranslucentAsync()
    {
        var s = await LoadAsync();
        return s.HudTranslucent;
    }

    public Task SetHudTranslucentAsync(bool value)
    {
        return SaveAsync(s => s.HudTranslucent = value);
    }

    public async Task<bool> GetAutoRecordBelowAltitudeAsync()
    {
        var s = await LoadAsync();
        return s.AutoRecordBelowAltitude;
    }

    public Task SetAutoRecordBelowAltitudeAsync(bool value)
    {
        return SaveAsync(s => s.AutoRecordBelowAltitude = value);
    }

    public async Task<int> GetAutoRecordAltitudeThresholdAsync()
    {
        var s = await LoadAsync();
        return s.AutoRecordAltitudeThreshold <= 0 ? 6000 : Math.Clamp(s.AutoRecordAltitudeThreshold, 1000, 15000);
    }

    public Task SetAutoRecordAltitudeThresholdAsync(int value)
    {
        var clamped = Math.Clamp(value, 1000, 15000);
        return SaveAsync(s => s.AutoRecordAltitudeThreshold = clamped);
    }

    public async Task<string> GetLanguageAsync()
    {
        var s = await LoadAsync();
        return string.IsNullOrWhiteSpace(s.Language) ? "en" : s.Language;
    }

    public Task SetLanguageAsync(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "en" : value.Trim();
        return SaveAsync(s => s.Language = normalized);
    }

    public async Task<bool> GetStayOnTopAsync()
    {
        var s = await LoadAsync();
        return s.StayOnTop;
    }

    public Task SetStayOnTopAsync(bool value)
    {
        return SaveAsync(s => s.StayOnTop = value);
    }

    public async Task<string> GetThemeAsync()
    {
        var s = await LoadAsync();
        return string.IsNullOrWhiteSpace(s.Theme) ? "dark" : s.Theme;
    }

    public Task SetThemeAsync(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "dark" : value.Trim();
        return SaveAsync(s => s.Theme = normalized);
    }

    private async Task<Settings> LoadAsync()
    {
        await sm.WaitAsync();
        try
        {
            return await LoadNoLockAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot read setting from {Path}", filePath);
            return settings ??= new Settings();
        }
        finally
        {
            sm.Release();
        }
    }

    private async Task SaveAsync(Action<Settings> update)
    {
        await sm.WaitAsync();
        try
        {
            var current = await LoadNoLockAsync();
            update(current);
            this.settings = current;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, current, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot write setting to {Path}", filePath);
        }
        finally
        {
            sm.Release();
        }
    }

    private async Task<Settings> LoadNoLockAsync()
    {
        if (settings != null)
        {
            return settings;
        }

        if (!File.Exists(filePath))
        {
            return settings = new Settings();
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var deserialized = await JsonSerializer.DeserializeAsync<Settings>(stream, jsonOptions);
            return settings = deserialized ?? new Settings();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read or parse {Path}, fallback to defaults", filePath);
            return settings = new Settings();
        }
    }
}

public class Settings
{
    public bool ShortcutKeysEnabled { get; set; } = true;
    public Dictionary<Shortcuts, ShortcutKey>? ShortcutKeys { get; set; } = null;
    public string? DefaultSaveFolder { get; set; } = null;
    public bool HudAutoHide { get; set; } = false;
    public bool HudTranslucent { get; set; } = false;
    public bool AutoRecordBelowAltitude { get; set; } = true;
    public int AutoRecordAltitudeThreshold { get; set; } = 6000;
    public string Language { get; set; } = "en";
    public bool StayOnTop { get; set; } = false;
    public string Theme { get; set; } = "dark";
}