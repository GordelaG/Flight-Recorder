using FlightRecorder.Client.SimConnectMSFS;
using System;
using System.Collections.Generic;

namespace FlightRecorder.Client.Logics;

public interface IReplayLogic
{
    event EventHandler<RecordsUpdatedEventArgs> RecordsUpdated;
    event EventHandler ReplayFinished;
    event EventHandler<CurrentFrameChangedEventArgs> CurrentFrameChanged;
    event EventHandler<AircraftProfile?>? ProfileChanged;

    AircraftProfile? CurrentProfile { get; }
    List<(long milliseconds, AircraftPositionStruct position)> Records { get; }
    List<CustomAircraftVariablesStruct?> CustomRecords { get; }
    string? AircraftTitle { get; set; }
    bool IsReplayable { get; }

    bool Replay();
    bool PauseReplay();
    bool ResumeReplay();
    void Seek(int value);
    void TrimStart();
    void TrimEnd();
    bool StopReplay();
    void ChangeRate(double rate);
    void SetRepeat(bool repeat);
    void Unfreeze();
    void NotifyPosition(AircraftPositionStruct? value);

    void FromData(string? fileName, SavedData data);
    SavedData ToData(string clientVersion);
}
