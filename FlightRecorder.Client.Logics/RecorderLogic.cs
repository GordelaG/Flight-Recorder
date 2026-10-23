using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FlightRecorder.Client.Logics;

public class RecorderLogic : IRecorderLogic, IDisposable
{
    public event EventHandler<RecordsUpdatedEventArgs>? RecordsUpdated;
    public event EventHandler<AircraftProfile?>? ProfileChanged;

    private readonly ILogger<RecorderLogic> logger;
    private readonly IConnector connector;
    private readonly AircraftProfileManager profileManager;
    private readonly Stopwatch stopwatch = new();

    private long? startMilliseconds;
    private long? endMilliseconds;
    private SimStateStruct startState;
    private List<(long milliseconds, AircraftPositionStruct position, CustomAircraftVariablesStruct? custom)> records = new();

    private SimStateStruct simState;
    private CustomAircraftVariablesStruct? latestCustomVariables;
    private AircraftProfile? currentProfile;

    public AircraftProfile? CurrentProfile => currentProfile;

    private bool IsStarted => startMilliseconds.HasValue && records != null;
    private bool IsEnded => startMilliseconds.HasValue && endMilliseconds.HasValue;

    public RecorderLogic(ILogger<RecorderLogic> logger, IConnector connector, AircraftProfileManager? profileManager = null)
    {
        logger.LogDebug("Creating instance of {class}", nameof(RecorderLogic));
        this.logger = logger;
        this.connector = connector;
        this.profileManager = profileManager ?? new AircraftProfileManager();

        connector.SimStateUpdated += Connector_SimStateUpdated;
        connector.CustomAircraftVariablesUpdated += Connector_CustomAircraftVariablesUpdated;
    }

    public void Dispose()
    {
        logger.LogDebug("Disposing {class}", nameof(RecorderLogic));
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            connector.SimStateUpdated -= Connector_SimStateUpdated;
            connector.CustomAircraftVariablesUpdated -= Connector_CustomAircraftVariablesUpdated;
        }
    }

    private void Connector_SimStateUpdated(object? sender, SimStateUpdatedEventArgs e)
    {
        simState = e.State;

        if (!IsStarted)
        {
            var matched = profileManager.FindProfileByTitle(simState.AircraftTitle);
            if (matched?.Id != currentProfile?.Id)
            {
                currentProfile = matched;
                logger.LogInformation("Detected profile {profile} for aircraft {aircraft}", currentProfile?.Name ?? "Generic", simState.AircraftTitle);
                connector.SetActiveProfile(currentProfile);
                ProfileChanged?.Invoke(this, currentProfile);
            }
        }
    }

    private void Connector_CustomAircraftVariablesUpdated(object? sender, CustomAircraftVariablesUpdatedEventArgs e)
    {
        latestCustomVariables = e.Variables;
    }

    #region Public Functions

    public void Initialize()
    {
        logger.LogDebug("Initializing recorder...");
        stopwatch.Start();
    }

    public void Record()
    {
        logger.LogInformation("Start recording...");

        startMilliseconds = stopwatch.ElapsedMilliseconds;
        endMilliseconds = null;
        startState = simState;
        records = new List<(long milliseconds, AircraftPositionStruct position, CustomAircraftVariablesStruct? custom)>();

        // Ensure active profile is locked in and connector is tracking
        if (currentProfile == null)
        {
            currentProfile = profileManager.FindProfileByTitle(startState.AircraftTitle);
            connector.SetActiveProfile(currentProfile);
            ProfileChanged?.Invoke(this, currentProfile);
        }
    }

    public void StopRecording()
    {
        if (endMilliseconds == null)
        {
            endMilliseconds = stopwatch.ElapsedMilliseconds;
            logger.LogDebug("Recording stopped. {totalFrames} frames recorded.", records.Count);
        }
    }

    public void NotifyPosition(AircraftPositionStruct? value)
    {
        if (IsStarted && !IsEnded && value.HasValue)
        {
            records.Add((stopwatch.ElapsedMilliseconds, value.Value, currentProfile != null ? latestCustomVariables : null));
            RecordsUpdated?.Invoke(this, new(null, startState.AircraftTitle, records.Count));
        }
    }

    public SavedData ToData(string clientVersion)
    {
        if (startMilliseconds == null) throw new InvalidOperationException("Cannot get data before started recording!");
        if (endMilliseconds == null) throw new InvalidOperationException("Cannot get data before finished recording!");
        return new(
            clientVersion,
            startMilliseconds.Value,
            endMilliseconds.Value,
            startState,
            records,
            currentProfile?.Id,
            currentProfile?.Variables.Count ?? 0
        );
    }

    #endregion
}
