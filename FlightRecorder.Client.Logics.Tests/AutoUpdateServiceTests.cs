using System;
using FlightRecorder.Client.Logics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlightRecorder.Client.Logics.Tests
{
    [TestClass]
    public class AutoUpdateServiceTests
    {
        [TestMethod]
        [DataRow("0.26.0", "v0.27.0", true)]
        [DataRow("0.26.0.0", "v0.27.1", true)]
        [DataRow("0.27.0", "v0.27.1", true)]
        [DataRow("0.27.0", "v0.27.0", false)]
        [DataRow("0.27.1", "v0.27.0", false)]
        [DataRow("0.27.0", "v0.27.0-beta", false)]
        [DataRow("0.27.0", "profiles-2026-10-23", false)]
        [DataRow("0.27.0", "", false)]
        public void Test_Version_Comparison(string current, string remote, bool expectedNewer)
        {
            var isNewer = AutoUpdateService.IsNewerVersion(current, remote);
            Assert.AreEqual(expectedNewer, isNewer, $"Comparison failed for current='{current}' vs remote='{remote}'");
        }

        [TestMethod]
        public void Test_Clean_Version_String()
        {
            Assert.AreEqual("0.27.1", AutoUpdateService.CleanVersionString("v0.27.1"));
            Assert.AreEqual("1.0.0", AutoUpdateService.CleanVersionString("1.0.0-rc1"));
            Assert.AreEqual("0.0.0.0", AutoUpdateService.CleanVersionString(""));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Test_Live_GitHub_Check()
        {
            var service = new AutoUpdateService();
            var result = await service.CheckForUpdatesAsync("0.27.0.0");
            Console.WriteLine($"IsUpdateAvailable={result.IsUpdateAvailable}, Latest={result.LatestVersion}, DownloadUrl={result.DownloadUrl}");
            Assert.IsNotNull(result);
        }
    }
}
