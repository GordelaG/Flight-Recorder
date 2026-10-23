using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlightRecorder.Client.Logics;

public class HubHopRawPreset
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("vendor")]
    public string Vendor { get; set; } = string.Empty;

    [JsonPropertyName("aircraft")]
    public string Aircraft { get; set; } = string.Empty;

    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("presetType")]
    public string PresetType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class HubHopSyncResult
{
    public bool Success { get; set; }
    public int TotalPresetsDownloaded { get; set; }
    public int ProfilesGenerated { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class HubHopSyncService
{
    private const string HubHopApiUrl = "https://hubhop-api-mgtm.azure-api.net/api/v1/msfs2020/presets";
    private static readonly Regex LVarRegex = new(@"\(\s*L:([A-Za-z0-9_.:]+)(?:,\s*([^)]+))?\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ILogger<HubHopSyncService>? logger;
    private readonly HttpClient httpClient;
    private readonly AircraftProfileManager profileManager;

    public HubHopSyncService(AircraftProfileManager profileManager, ILogger<HubHopSyncService>? logger = null, HttpClient? httpClient = null)
    {
        this.profileManager = profileManager;
        this.logger = logger;
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task<HubHopSyncResult> SyncFromApiAsync(IProgress<string>? progress = null, string? targetFolder = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("Conectando à base de dados do HubHop...");
            logger?.LogInformation("Connecting to HubHop API at {url}", HubHopApiUrl);

            using var response = await httpClient.GetAsync(HubHopApiUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            progress?.Report("Baixando catálogo de presets do HubHop...");
            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var presets = await JsonSerializer.DeserializeAsync<List<HubHopRawPreset>>(jsonStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, cancellationToken);

            if (presets == null || presets.Count == 0)
            {
                return new HubHopSyncResult { Success = false, Message = "Nenhum preset retornado pelo HubHop." };
            }

            progress?.Report($"Processando {presets.Count:N0} presets e categorizando aeronaves...");
            logger?.LogInformation("Downloaded {count} presets from HubHop. Processing aircraft profiles...", presets.Count);

            var generatedCount = ProcessAndSavePresets(presets, targetFolder);
            profileManager.ReloadProfiles();

            progress?.Report($"Concluído! {generatedCount} perfis de aeronaves atualizados com sucesso.");
            return new HubHopSyncResult
            {
                Success = true,
                TotalPresetsDownloaded = presets.Count,
                ProfilesGenerated = generatedCount,
                Message = $"Sincronização bem-sucedida! {generatedCount} perfis gerados a partir de {presets.Count:N0} presets do HubHop."
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to sync HubHop presets from API");
            return new HubHopSyncResult
            {
                Success = false,
                Message = $"Erro ao sincronizar com o HubHop: {ex.Message}"
            };
        }
    }

    public int ProcessAndSavePresets(IEnumerable<HubHopRawPreset> presets, string? outputFolder = null)
    {
        var targetDir = outputFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aircraft_profiles");
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Group by Vendor + Aircraft, ignoring generic or empty
        var groups = presets
            .Where(p => !string.IsNullOrWhiteSpace(p.Vendor) &&
                        !string.IsNullOrWhiteSpace(p.Aircraft) &&
                        !p.Vendor.Equals("Generic", StringComparison.OrdinalIgnoreCase) &&
                        !p.Aircraft.Equals("Generic", StringComparison.OrdinalIgnoreCase) &&
                        (p.PresetType?.Contains("Output", StringComparison.OrdinalIgnoreCase) == true ||
                         p.Code.Contains("L:", StringComparison.OrdinalIgnoreCase)))
            .GroupBy(p => $"{p.Vendor.Trim()}___{p.Aircraft.Trim()}");

        int profilesWritten = 0;

        foreach (var group in groups)
        {
            var parts = group.Key.Split(new[] { "___" }, StringSplitOptions.None);
            var vendor = parts[0];
            var aircraft = parts[1];

            var profileId = SanitizeId($"{vendor}_{aircraft}");
            var profileName = $"{vendor} {aircraft}";

            // Extract readable L-Vars
            var candidateVars = new List<(AircraftVariable variable, int priority)>();
            var seenVarNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var preset in group)
            {
                var match = LVarRegex.Match(preset.Code);
                if (!match.Success) continue;

                var rawVarName = match.Groups[1].Value.Trim();
                var varName = rawVarName.StartsWith("L:", StringComparison.OrdinalIgnoreCase) ? rawVarName : $"L:{rawVarName}";
                var unit = match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[2].Value.Trim() : "number";

                if (seenVarNames.Contains(varName)) continue;
                seenVarNames.Add(varName);

                var (priority, interpolate) = CategorizeVariable(preset.Label, preset.System, preset.Description, varName);
                if (priority > 0)
                {
                    candidateVars.Add((new AircraftVariable(varName, unit, preset.Label, interpolate), priority));
                }
            }

            if (candidateVars.Count == 0) continue;

            // Pick up to MaxVariables (32) sorted by priority
            var selectedVars = candidateVars
                .OrderByDescending(c => c.priority)
                .ThenBy(c => c.variable.Name)
                .Take(CustomAircraftVariablesStruct.MaxVariables)
                .Select(c => c.variable)
                .ToList();

            var matchTitles = GenerateMatchTitles(vendor, aircraft);

            var profile = new AircraftProfile(profileId, profileName, vendor, matchTitles, selectedVars);

            // Write JSON file
            try
            {
                var filePath = Path.Combine(targetDir, $"{profileId}.json");
                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                profilesWritten++;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to write profile for {profileId}", profileId);
            }
        }

        return profilesWritten;
    }

    public static (int priority, bool interpolate) CategorizeVariable(string label, string system, string desc, string varName)
    {
        var text = $"{label} {system} {desc} {varName}".ToLowerInvariant();

        // High priority: Flaps, Slats, Spoilers, Reversers, Gear
        if (text.Contains("flap") || text.Contains("slat") || text.Contains("trailing edge") || text.Contains("leading edge"))
        {
            bool interpolate = text.Contains("angle") || text.Contains("pct") || text.Contains("percent") || text.Contains("pos");
            return (100, interpolate);
        }
        if (text.Contains("spoiler") || text.Contains("speedbrake") || text.Contains("speed brake"))
        {
            return (95, true);
        }
        if (text.Contains("reverse") || text.Contains("reverser"))
        {
            return (90, true);
        }
        if (text.Contains("gear") && (text.Contains("lever") || text.Contains("handle") || text.Contains("down") || text.Contains("nose")))
        {
            return (85, false);
        }
        if (text.Contains("throttle") || text.Contains("thrust"))
        {
            return (80, true);
        }
        if (text.Contains("aileron") || text.Contains("elevator") || text.Contains("rudder") || text.Contains("deflection"))
        {
            return (75, true);
        }
        if (text.Contains("brake") && (text.Contains("park") || text.Contains("auto")))
        {
            return (70, false);
        }
        // Medium priority: Exterior lights
        if (text.Contains("light") || text.Contains("strobe") || text.Contains("beacon") || text.Contains("landing") || text.Contains("taxi") || text.Contains("nav"))
        {
            return (60, false);
        }
        // Autopilot / Annunciators
        if (text.Contains("autopilot") || text.Contains("mcp") || text.Contains("fcu"))
        {
            return (40, false);
        }

        // Generic other L-Var
        return (20, false);
    }

    public static List<string> GenerateMatchTitles(string vendor, string aircraft)
    {
        var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        list.Add($"{vendor} {aircraft}");
        list.Add(aircraft);

        // Normalize variations (e.g. "PMDG 737", "FlyByWire A32NX", etc.)
        if (aircraft.Contains("A320", StringComparison.OrdinalIgnoreCase)) list.Add("A320");
        if (aircraft.Contains("A32NX", StringComparison.OrdinalIgnoreCase)) list.Add("A32NX");
        if (aircraft.Contains("737", StringComparison.OrdinalIgnoreCase)) list.Add("737");
        if (aircraft.Contains("777", StringComparison.OrdinalIgnoreCase)) list.Add("777");
        if (aircraft.Contains("A330", StringComparison.OrdinalIgnoreCase)) list.Add("A330");
        if (aircraft.Contains("A380", StringComparison.OrdinalIgnoreCase)) list.Add("A380");
        if (aircraft.Contains("CRJ", StringComparison.OrdinalIgnoreCase)) list.Add("CRJ");
        if (aircraft.Contains("MD-11", StringComparison.OrdinalIgnoreCase) || aircraft.Contains("MD11", StringComparison.OrdinalIgnoreCase)) list.Add("MD-11");
        if (aircraft.Contains("Concorde", StringComparison.OrdinalIgnoreCase)) list.Add("Concorde");

        return list.ToList();
    }

    public static string SanitizeId(string raw)
    {
        var clean = Regex.Replace(raw, @"[^A-Za-z0-9_]+", "_").Trim('_');
        return clean.ToLowerInvariant();
    }
}
