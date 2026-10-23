using FlightRecorder.Client.SimConnectMSFS;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FlightRecorder.Client.Logics;

public class SavedData
{
    public SavedData(string clientVersion, long startTime, long endTime, SimStateStruct? simState, List<(long milliseconds, AircraftPositionStruct position)> records)
        : this(clientVersion, startTime, endTime, simState, records.Select(r => (r.milliseconds, r.position, (CustomAircraftVariablesStruct?)null)).ToList(), null, 0)
    {
    }

    public SavedData(string clientVersion, long startTime, long endTime, SimStateStruct? simState,
        List<(long milliseconds, AircraftPositionStruct position, CustomAircraftVariablesStruct? custom)> records,
        string? profileId = null, int customVarCount = 0)
    {
        ClientVersion = clientVersion;
        StartTime = startTime;
        EndTime = endTime;
        ProfileId = profileId;
        StartState = simState.HasValue ? SimState.FromStruct(simState.Value) : null;
        Records = records.Select(r => new SavedRecord
        (
            r.milliseconds,
            AircraftPosition.FromStruct(r.position),
            r.custom.HasValue && customVarCount > 0 ? r.custom.Value.ToArray(customVarCount) : null
        )).ToList();
    }

    [JsonConstructor]
    public SavedData(string clientVersion, long startTime, long endTime, SimState? startState, List<SavedRecord>? records, string? profileId = null)
    {
        ClientVersion = clientVersion;
        StartTime = startTime;
        EndTime = endTime;
        StartState = startState;
        ProfileId = profileId;
        Records = records ?? new List<SavedRecord>();
    }

    public string ClientVersion { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public string? ProfileId { get; set; }
    public SimState? StartState { get; set; }
    public List<SavedRecord> Records { get; set; }

    public class SavedRecord
    {
        public SavedRecord(long time, AircraftPosition position)
        {
            Time = time;
            Position = position;
        }

        [JsonConstructor]
        public SavedRecord(long time, AircraftPosition position, double[]? customVariables = null)
        {
            Time = time;
            Position = position;
            CustomVariables = customVariables;
        }

        public long Time { get; set; }
        public AircraftPosition Position { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double[]? CustomVariables { get; set; }
    }
}
