using FlightRecorder.Client.SimConnectMSFS;
using System;

namespace FlightRecorder.Client.Logics;

public interface IRecorderLogic
{
    event EventHandler<RecordsUpdatedEventArgs> RecordsUpdated;
    event EventHandler<AircraftProfile?>? ProfileChanged;

    AircraftProfile? CurrentProfile { get; }

    void Initialize();
    void Record();
    void StopRecording();
    void NotifyPosition(AircraftPositionStruct? value);

    SavedData ToData(string clientVersion);
}