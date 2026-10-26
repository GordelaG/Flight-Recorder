using FlightRecorder.Client.Logic;
using FlightRecorder.Client.Logics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FlightRecorder.Client.Logics.Tests;

[TestClass]
public class SettingsLogicTests
{
    private string testFilePath = null!;

    [TestInitialize]
    public void Setup()
    {
        testFilePath = Path.Combine(Path.GetTempPath(), "FlightRecorder_SettingsTest_" + Guid.NewGuid().ToString("N"), "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            var dir = Path.GetDirectoryName(testFilePath);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        catch { }
    }

    [TestMethod]
    public async Task TestDefaultValues_WhenFileDoesNotExist()
    {
        var logic = new FileSettingsLogic(NullLogger<FileSettingsLogic>.Instance, testFilePath);

        var lang = await logic.GetLanguageAsync();
        var stayOnTop = await logic.GetStayOnTopAsync();
        var theme = await logic.GetThemeAsync();
        var autoRec = await logic.GetAutoRecordBelowAltitudeAsync();
        var thresh = await logic.GetAutoRecordAltitudeThresholdAsync();
        var shortcutsEnabled = await logic.IsShortcutKeysEnabledAsync();
        var hudAutoHide = await logic.GetHudAutoHideAsync();
        var hudTranslucent = await logic.GetHudTranslucentAsync();

        Assert.AreEqual("en", lang);
        Assert.IsFalse(stayOnTop);
        Assert.AreEqual("dark", theme);
        Assert.IsTrue(autoRec);
        Assert.AreEqual(6000, thresh);
        Assert.IsTrue(shortcutsEnabled);
        Assert.IsFalse(hudAutoHide);
        Assert.IsFalse(hudTranslucent);
    }

    [TestMethod]
    public async Task TestSettingsPersistence_AcrossInstances_SimulatingAppRestart()
    {
        // 1. Initial app run: change settings
        var logic1 = new FileSettingsLogic(NullLogger<FileSettingsLogic>.Instance, testFilePath);

        await logic1.SetLanguageAsync("pt-BR");
        await logic1.SetStayOnTopAsync(true);
        await logic1.SetThemeAsync("light");
        await logic1.SetAutoRecordBelowAltitudeAsync(false);
        await logic1.SetAutoRecordAltitudeThresholdAsync(4500);
        await logic1.SetHudAutoHideAsync(true);
        await logic1.SetHudTranslucentAsync(true);
        await logic1.SetShortcutKeysEnabledAsync(false);
        await logic1.SetDefaultSaveFolderAsync(@"C:\TestFlights");

        var customShortcuts = new Dictionary<Shortcuts, ShortcutKey>
        {
            [Shortcuts.Record] = new ShortcutKey(true, false, true, "R", 82)
        };
        await logic1.SetShortcutKeysAsync(customShortcuts);

        Assert.IsTrue(File.Exists(testFilePath), "settings.json file should have been written to disk");

        // 2. Simulated app restart: new instance reading from the same file
        var logic2 = new FileSettingsLogic(NullLogger<FileSettingsLogic>.Instance, testFilePath);

        Assert.AreEqual("pt-BR", await logic2.GetLanguageAsync());
        Assert.IsTrue(await logic2.GetStayOnTopAsync());
        Assert.AreEqual("light", await logic2.GetThemeAsync());
        Assert.IsFalse(await logic2.GetAutoRecordBelowAltitudeAsync());
        Assert.AreEqual(4500, await logic2.GetAutoRecordAltitudeThresholdAsync());
        Assert.IsTrue(await logic2.GetHudAutoHideAsync());
        Assert.IsTrue(await logic2.GetHudTranslucentAsync());
        Assert.IsFalse(await logic2.IsShortcutKeysEnabledAsync());
        Assert.AreEqual(@"C:\TestFlights", await logic2.GetDefaultSaveFolderAsync());

        var loadedShortcuts = await logic2.GetShortcutKeysAsync();
        Assert.IsNotNull(loadedShortcuts);
        Assert.IsTrue(loadedShortcuts.ContainsKey(Shortcuts.Record));
        Assert.AreEqual("R", loadedShortcuts[Shortcuts.Record].Key);
        Assert.IsTrue(loadedShortcuts[Shortcuts.Record].Ctrl);
        Assert.IsTrue(loadedShortcuts[Shortcuts.Record].Shift);
        Assert.IsFalse(loadedShortcuts[Shortcuts.Record].Alt);
    }

    [TestMethod]
    public async Task TestAutoRecordAltitudeThreshold_Clamping()
    {
        var logic = new FileSettingsLogic(NullLogger<FileSettingsLogic>.Instance, testFilePath);

        await logic.SetAutoRecordAltitudeThresholdAsync(500);
        Assert.AreEqual(1000, await logic.GetAutoRecordAltitudeThresholdAsync());

        await logic.SetAutoRecordAltitudeThresholdAsync(20000);
        Assert.AreEqual(15000, await logic.GetAutoRecordAltitudeThresholdAsync());

        await logic.SetAutoRecordAltitudeThresholdAsync(8500);
        Assert.AreEqual(8500, await logic.GetAutoRecordAltitudeThresholdAsync());
    }

    [TestMethod]
    public async Task TestLanguage_EmptyFallback()
    {
        var logic = new FileSettingsLogic(NullLogger<FileSettingsLogic>.Instance, testFilePath);

        await logic.SetLanguageAsync("   ");
        Assert.AreEqual("en", await logic.GetLanguageAsync());

        await logic.SetLanguageAsync("pt-BR");
        Assert.AreEqual("pt-BR", await logic.GetLanguageAsync());
    }

    [TestMethod]
    public void TestResolveSettingsFilePath_PointsToLocalAppData()
    {
        var path = FileSettingsLogic.ResolveSettingsFilePath();
        Assert.IsTrue(path.Contains("FlightRecorder"));
        Assert.IsTrue(path.EndsWith("settings.json"));
        Assert.IsTrue(path.Contains(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
    }
}
