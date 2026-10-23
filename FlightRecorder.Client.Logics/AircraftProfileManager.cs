using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FlightRecorder.Client.Logics;

public class AircraftProfileManager
{
    private readonly ILogger<AircraftProfileManager>? logger;
    private readonly string profilesDirectory;
    private readonly List<AircraftProfile> profiles = new();

    public IReadOnlyList<AircraftProfile> Profiles => profiles.AsReadOnly();

    public AircraftProfileManager(ILogger<AircraftProfileManager>? logger = null, string? customProfilesDirectory = null)
    {
        this.logger = logger;
        this.profilesDirectory = customProfilesDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aircraft_profiles");
        RegisterBuiltInProfiles();
        LoadCustomProfilesFromDisk();
    }

    public AircraftProfile? FindProfileByTitle(string? aircraftTitle)
    {
        if (string.IsNullOrWhiteSpace(aircraftTitle)) return null;

        var lower = aircraftTitle.ToLowerInvariant();
        return profiles.FirstOrDefault(p =>
            p.MatchTitles.Any(match => lower.Contains(match.ToLowerInvariant())));
    }

    public AircraftProfile? GetProfileById(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId)) return null;
        return profiles.FirstOrDefault(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));
    }

    public void ReloadProfiles()
    {
        profiles.Clear();
        RegisterBuiltInProfiles();
        LoadCustomProfilesFromDisk();
        logger?.LogInformation("Aircraft profiles reloaded. Total profiles: {count}", profiles.Count);
    }

    private void RegisterBuiltInProfiles()
    {
        // 1. FlyByWire A32NX & Headwind A330 Family
        profiles.Add(new AircraftProfile(
            id: "FBW_A32NX",
            name: "FlyByWire A32NX & A330",
            vendor: "FlyByWire Simulations",
            matchTitles: new[] { "A32NX", "FlyByWire", "A330-900", "Headwind", "A380X" },
            variables: new[]
            {
                new AircraftVariable("L:A32NX_FLAPS_CONF_INDEX", "number", "Flaps Configuration Index (0-5)", false),
                new AircraftVariable("L:A32NX_LEFT_FLAPS_ANGLE", "degrees", "Left Flaps Deflection Angle", true),
                new AircraftVariable("L:A32NX_RIGHT_FLAPS_ANGLE", "degrees", "Right Flaps Deflection Angle", true),
                new AircraftVariable("L:A32NX_SPOILERS_HANDLE_POSITION", "number", "Spoiler Handle Position (0-1)", true),
                new AircraftVariable("L:A32NX_SPOILERS_ARMED", "number", "Ground Spoilers Armed", false),
                new AircraftVariable("L:A32NX_THROTTLE_MAPPING_LEVER_MODE:1", "number", "Throttle Mode Eng 1", false),
                new AircraftVariable("L:A32NX_THROTTLE_MAPPING_LEVER_MODE:2", "number", "Throttle Mode Eng 2", false),
                new AircraftVariable("L:A32NX_REVERSE_THRUST_ENG1", "number", "Reverse Thrust Eng 1", true),
                new AircraftVariable("L:A32NX_REVERSE_THRUST_ENG2", "number", "Reverse Thrust Eng 2", true),
                new AircraftVariable("L:A32NX_GEAR_HANDLE_POSITION", "number", "Gear Handle Position", false),
                new AircraftVariable("L:A32NX_NOSE_GEAR_ANGLE", "degrees", "Nose Wheel Steering Angle", true)
            }
        ));

        // 2. PMDG 737 Family (-600 / -700 / -800 / -900)
        profiles.Add(new AircraftProfile(
            id: "PMDG_737",
            name: "PMDG 737 NG3 Series",
            vendor: "PMDG Simulations",
            matchTitles: new[] { "PMDG 737", "PMDG 738", "PMDG 739", "PMDG 736", "PMDG 73-800", "PMDG 73-700", "PMDG 73-600", "PMDG 73-900" },
            variables: new[]
            {
                new AircraftVariable("L:switch_115_73X", "number", "Flap Lever Position", false),
                new AircraftVariable("L:switch_114_73X", "number", "Speed Brake / Spoiler Lever Position", true),
                new AircraftVariable("L:switch_120_73X", "number", "Landing Gear Lever Position", false),
                new AircraftVariable("L:switch_1001_73X", "number", "Thrust Reverser Left", true),
                new AircraftVariable("L:switch_1002_73X", "number", "Thrust Reverser Right", true),
                new AircraftVariable("L:switch_111_73X", "number", "Parking Brake Lever", false),
                new AircraftVariable("L:switch_112_73X", "number", "Autobrake Selector Switch", false),
                new AircraftVariable("L:switch_142_73X", "number", "Retractable Landing Light Left", false),
                new AircraftVariable("L:switch_143_73X", "number", "Retractable Landing Light Right", false),
                new AircraftVariable("L:switch_144_73X", "number", "Fixed Landing Light Left", false),
                new AircraftVariable("L:switch_145_73X", "number", "Fixed Landing Light Right", false),
                new AircraftVariable("L:switch_146_73X", "number", "Runway Turnoff Light Left", false),
                new AircraftVariable("L:switch_147_73X", "number", "Runway Turnoff Light Right", false),
                new AircraftVariable("L:switch_148_73X", "number", "Taxi Light", false),
                new AircraftVariable("L:switch_150_73X", "number", "Strobe Light", false),
                new AircraftVariable("L:switch_151_73X", "number", "Anti-Collision / Beacon Light", false),
                new AircraftVariable("L:switch_152_73X", "number", "Wing Light", false),
                new AircraftVariable("L:switch_154_73X", "number", "Logo Light", false)
            }
        ));

        // 3. Fenix A320 Family
        profiles.Add(new AircraftProfile(
            id: "FENIX_A320",
            name: "Fenix A320 Family",
            vendor: "Fenix Simulations",
            matchTitles: new[] { "Fenix", "FNX", "FenixA320" },
            variables: new[]
            {
                new AircraftVariable("L:S_FC_FLAP_LEVER", "number", "Flap Lever Position", false),
                new AircraftVariable("L:S_FC_SPEED_BRAKE", "number", "Speed Brake Lever Position", true),
                new AircraftVariable("L:S_LG_GEAR_LEVER", "number", "Landing Gear Lever", false),
                new AircraftVariable("L:S_ENG_THROTTLE_1", "number", "Throttle Lever 1", true),
                new AircraftVariable("L:S_ENG_THROTTLE_2", "number", "Throttle Lever 2", true),
                new AircraftVariable("L:S_ENG_REVERSER_1", "number", "Reverser 1", true),
                new AircraftVariable("L:S_ENG_REVERSER_2", "number", "Reverser 2", true),
                new AircraftVariable("L:S_EXT_LT_LANDING_L", "number", "Landing Light Left", false),
                new AircraftVariable("L:S_EXT_LT_LANDING_R", "number", "Landing Light Right", false),
                new AircraftVariable("L:S_EXT_LT_NOSE", "number", "Nose Taxi/Takeoff Light", false),
                new AircraftVariable("L:S_EXT_LT_STROBE", "number", "Strobe Light", false),
                new AircraftVariable("L:S_EXT_LT_BEACON", "number", "Beacon Light", false)
            }
        ));

        // 4. iniBuilds Airliners (A300, A310, A320neo v2)
        profiles.Add(new AircraftProfile(
            id: "INIBUILDS_AIRLINERS",
            name: "iniBuilds Airliners (A300/A310/A320)",
            vendor: "iniBuilds",
            matchTitles: new[] { "iniBuilds", "A310", "A300", "A320neo (v2)" },
            variables: new[]
            {
                new AircraftVariable("L:INI_FLAPS_LEVER", "number", "Flaps Lever Position", false),
                new AircraftVariable("L:INI_SPOILER_LEVER", "number", "Spoiler Lever Position", true),
                new AircraftVariable("L:INI_GEAR_LEVER", "number", "Landing Gear Lever", false),
                new AircraftVariable("L:INI_REVERSER_1", "number", "Engine 1 Reverser", true),
                new AircraftVariable("L:INI_REVERSER_2", "number", "Engine 2 Reverser", true)
            }
        ));
    }

    private void LoadCustomProfilesFromDisk()
    {
        try
        {
            var profilesDir = profilesDirectory;
            if (!Directory.Exists(profilesDir))
            {
                Directory.CreateDirectory(profilesDir);
                return;
            }

            var files = Directory.GetFiles(profilesDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<AircraftProfile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.Id) && profile.Variables.Count > 0)
                    {
                        var existingIndex = profiles.FindIndex(p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
                        if (existingIndex >= 0)
                        {
                            profiles[existingIndex] = profile;
                        }
                        else
                        {
                            profiles.Add(profile);
                        }
                        logger?.LogInformation("Loaded custom aircraft profile {profileName} from {file}", profile.Name, Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to load custom aircraft profile from {file}", file);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to inspect custom aircraft_profiles folder");
        }
    }
}
