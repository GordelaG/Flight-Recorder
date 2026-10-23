using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text.Json;

namespace FlightRecorder.Client.Logics.Tests;

[TestClass]
public class AircraftProfileTests
{
    [TestMethod]
    public void TestProfileDetection()
    {
        var manager = new AircraftProfileManager();

        var fbw = manager.FindProfileByTitle("Airbus A320neo FlyByWire");
        Assert.IsNotNull(fbw);
        Assert.AreEqual("FBW_A32NX", fbw.Id);
        Assert.IsTrue(fbw.Variables.Count > 5);

        var pmdg = manager.FindProfileByTitle("PMDG 737-800 GOL Linhas Aereas");
        Assert.IsNotNull(pmdg);
        Assert.AreEqual("PMDG_737", pmdg.Id);
        Assert.IsTrue(pmdg.Variables.Count > 10);

        var fenix = manager.FindProfileByTitle("FenixA320 - LATAM Brasil");
        Assert.IsNotNull(fenix);
        Assert.AreEqual("FENIX_A320", fenix.Id);

        var cessna = manager.FindProfileByTitle("Cessna 172 Skyhawk G1000");
        Assert.IsNull(cessna);
    }

    [TestMethod]
    public void TestCustomVariablesStruct()
    {
        var s = new CustomAircraftVariablesStruct();
        s[0] = 3.0; // e.g. Flaps index
        s[1] = 0.75; // e.g. Spoilers
        s[31] = 1.0;

        Assert.AreEqual(3.0, s[0]);
        Assert.AreEqual(0.75, s[1]);
        Assert.AreEqual(1.0, s[31]);

        var arr = s.ToArray(3);
        Assert.AreEqual(3, arr.Length);
        Assert.AreEqual(3.0, arr[0]);
        Assert.AreEqual(0.75, arr[1]);
        Assert.AreEqual(0.0, arr[2]);

        var reconstructed = CustomAircraftVariablesStruct.FromArray(new double[] { 5.0, 10.0 });
        Assert.AreEqual(5.0, reconstructed[0]);
        Assert.AreEqual(10.0, reconstructed[1]);
        Assert.AreEqual(0.0, reconstructed[2]);
    }

    [TestMethod]
    public void TestSavedDataWithCustomVariablesSerialization()
    {
        var pos = new AircraftPositionStruct { Latitude = -23.5, Longitude = -46.6, Altitude = 2500 };
        var custom = new CustomAircraftVariablesStruct();
        custom[0] = 2.0; // Flaps
        custom[1] = 0.5; // Spoilers

        var records = new List<(long milliseconds, AircraftPositionStruct position, CustomAircraftVariablesStruct? custom)>
        {
            (1000, pos, custom)
        };

        var simState = new SimStateStruct { AircraftTitle = "PMDG 737-800" };
        var savedData = new SavedData("1.0", 1000, 2000, simState, records, "PMDG_737", 2);

        var json = JsonSerializer.Serialize(savedData);
        Assert.IsTrue(json.Contains("\"ProfileId\":\"PMDG_737\""));
        Assert.IsTrue(json.Contains("\"CustomVariables\":[2,0.5]"));

        var deserialized = JsonSerializer.Deserialize<SavedData>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual("PMDG_737", deserialized.ProfileId);
        Assert.AreEqual(1, deserialized.Records.Count);
        Assert.IsNotNull(deserialized.Records[0].CustomVariables);
        Assert.AreEqual(2.0, deserialized.Records[0].CustomVariables![0]);
        Assert.AreEqual(0.5, deserialized.Records[0].CustomVariables![1]);
    }

    [TestMethod]
    public void TestHubHopCategorization()
    {
        var (flapsPrio, flapsInterp) = HubHopSyncService.CategorizeVariable("Flaps Handle Index", "Flight Controls", "Current flaps notch", "L:A32NX_FLAPS_CONF_INDEX");
        Assert.IsTrue(flapsPrio >= 100);

        var (spoilerPrio, spoilerInterp) = HubHopSyncService.CategorizeVariable("Left Ground Spoilers", "Surfaces", "Speedbrake position", "L:A32NX_SPOILERS_LEFT");
        Assert.IsTrue(spoilerPrio >= 90);
        Assert.IsTrue(spoilerInterp);

        var (revPrio, revInterp) = HubHopSyncService.CategorizeVariable("Reverse Thrust Deploy", "Engines", "Reverser angle", "L:REV_DEPLOY");
        Assert.IsTrue(revPrio >= 90);
        Assert.IsTrue(revInterp);

        var (lightPrio, _) = HubHopSyncService.CategorizeVariable("Strobe Light Switch", "Lights", "Strobe toggle", "L:LIGHTING_STROBE");
        Assert.IsTrue(lightPrio >= 60);

        var (unknownPrio, _) = HubHopSyncService.CategorizeVariable("Random Switch", "Cabin", "Coffee maker toggle", "L:COFFEE_MAKER");
        Assert.IsTrue(unknownPrio <= 20);
    }

    [TestMethod]
    public void TestHubHopPresetProcessing()
    {
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FR_HubHop_Test_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            var presets = new List<HubHopRawPreset>
            {
                new HubHopRawPreset
                {
                    Vendor = "FlyByWire",
                    Aircraft = "A380X",
                    System = "Surfaces",
                    Label = "Left Inboard Flaps",
                    Code = "(L:A380X_FLAPS_INBOARD_L, Percent)",
                    PresetType = "Output"
                },
                new HubHopRawPreset
                {
                    Vendor = "FlyByWire",
                    Aircraft = "A380X",
                    System = "Engines",
                    Label = "Reverser 1 Deploy",
                    Code = "(L:A380X_THRUST_REV_1_POS, Percent)",
                    PresetType = "Output"
                },
                new HubHopRawPreset
                {
                    Vendor = "FlyByWire",
                    Aircraft = "A380X",
                    System = "Surfaces",
                    Label = "Speedbrake Handle",
                    Code = "(L:A380X_SPEEDBRAKE_POS, Percent)",
                    PresetType = "Output"
                }
            };

            var profileManager = new AircraftProfileManager(customProfilesDirectory: tempDir);
            var syncService = new HubHopSyncService(profileManager);

            int written = syncService.ProcessAndSavePresets(presets, tempDir);
            Assert.AreEqual(1, written);

            var generatedFile = System.IO.Path.Combine(tempDir, "flybywire_a380x.json");
            Assert.IsTrue(System.IO.File.Exists(generatedFile));

            var json = System.IO.File.ReadAllText(generatedFile);
            var profile = JsonSerializer.Deserialize<AircraftProfile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.IsNotNull(profile);
            Assert.AreEqual("flybywire_a380x", profile.Id);
            Assert.AreEqual("FlyByWire A380X", profile.Name);
            Assert.AreEqual("FlyByWire", profile.Vendor);
            Assert.AreEqual(3, profile.Variables.Count);

            // Flaps should be highest priority first
            Assert.AreEqual("L:A380X_FLAPS_INBOARD_L", profile.Variables[0].Name);
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.Delete(tempDir, true);
            }
        }
    }
}
