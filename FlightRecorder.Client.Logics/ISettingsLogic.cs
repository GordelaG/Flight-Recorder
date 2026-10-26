using FlightRecorder.Client.Logic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightRecorder.Client.Logics;

public interface ISettingsLogic
{
    Task<bool> IsShortcutKeysEnabledAsync();
    Task SetShortcutKeysEnabledAsync(bool value);
    Task<Dictionary<Shortcuts, ShortcutKey>?> GetShortcutKeysAsync();
    Task SetShortcutKeysAsync(Dictionary<Shortcuts, ShortcutKey> shortcuts);
    Task<string?> GetDefaultSaveFolderAsync();
    Task SetDefaultSaveFolderAsync(string? folderPath);
    Task<bool> GetHudAutoHideAsync();
    Task SetHudAutoHideAsync(bool value);
    Task<bool> GetHudTranslucentAsync();
    Task SetHudTranslucentAsync(bool value);
    Task<bool> GetAutoRecordBelowAltitudeAsync();
    Task SetAutoRecordBelowAltitudeAsync(bool value);
    Task<int> GetAutoRecordAltitudeThresholdAsync();
    Task SetAutoRecordAltitudeThresholdAsync(int value);
    Task<string> GetLanguageAsync();
    Task SetLanguageAsync(string value);
    Task<bool> GetStayOnTopAsync();
    Task SetStayOnTopAsync(bool value);
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string value);
}
