using FlightRecorder.Client.Logics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FlightRecorder.Client.Logics.Tests;

[TestClass]
public class SavedFlightsLogicTests
{
    [TestMethod]
    public void TestSanitizeFileName_RemovesInvalidCharacters()
    {
        var sanitized = SavedFlightsLogic.SanitizeFileName("PMDG: 737 / 800 * ? <test>");
        Assert.IsFalse(sanitized.Contains(':'));
        Assert.IsFalse(sanitized.Contains('/'));
        Assert.IsFalse(sanitized.Contains('*'));
        Assert.IsFalse(sanitized.Contains('?'));
        Assert.IsFalse(sanitized.Contains('<'));
        Assert.IsFalse(sanitized.Contains('>'));
    }

    [TestMethod]
    public void TestGetSavedFlightsDirectory_ReturnsValidDirectory()
    {
        var dir = SavedFlightsLogic.GetSavedFlightsDirectory();
        Assert.IsFalse(string.IsNullOrWhiteSpace(dir));
        Assert.IsTrue(Directory.Exists(dir));
    }

    [TestMethod]
    public async Task TestSaveMetadataAndRetrieve()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FlightRecorder_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var flightFile = Path.Combine(tempDir, "TestFlight_123.fltrec");
            await File.WriteAllTextAsync(flightFile, "dummy zip content");

            var savedData = new SavedData(
                clientVersion: "0.27.6",
                startTime: 1000,
                endTime: 5000,
                startState: (SimState?)null,
                records: new List<SavedData.SavedRecord>
                {
                    new SavedData.SavedRecord(1000, new AircraftPosition()),
                    new SavedData.SavedRecord(5000, new AircraftPosition())
                },
                profileId: "pmdg-737"
            );

            await SavedFlightsLogic.SaveMetadataAsync(flightFile, savedData, "Pouso Congonhas 17R");

            var metaFile = flightFile + ".meta.json";
            Assert.IsTrue(File.Exists(metaFile));

            var metaJson = await File.ReadAllTextAsync(metaFile);
            var item = System.Text.Json.JsonSerializer.Deserialize<SavedFlightItem>(metaJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.IsNotNull(item);
            Assert.AreEqual("Pouso Congonhas 17R", item.Title);
            Assert.AreEqual("Flight", item.AircraftTitle);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [TestMethod]
    public async Task TestDeleteSavedFlight()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test_delete_" + Guid.NewGuid().ToString("N") + ".fltrec");
        var tempMeta = tempFile + ".meta.json";

        await File.WriteAllTextAsync(tempFile, "data");
        await File.WriteAllTextAsync(tempMeta, "{}");

        Assert.IsTrue(File.Exists(tempFile));
        Assert.IsTrue(File.Exists(tempMeta));

        var deleted = SavedFlightsLogic.DeleteSavedFlight(tempFile);
        Assert.IsTrue(deleted);
        Assert.IsFalse(File.Exists(tempFile));
        Assert.IsFalse(File.Exists(tempMeta));
    }
}
