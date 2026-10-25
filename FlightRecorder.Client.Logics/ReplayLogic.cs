using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace FlightRecorder.Client.Logics;

public class ReplayLogic : IReplayLogic, IDisposable
{
    public event EventHandler<RecordsUpdatedEventArgs>? RecordsUpdated;
    public event EventHandler? ReplayFinished;
    public event EventHandler<CurrentFrameChangedEventArgs>? CurrentFrameChanged;
    public event EventHandler<AircraftProfile?>? ProfileChanged;

    private const int EventThrottleMilliseconds = 500;

    private readonly ILogger<ReplayLogic> logger;
    private readonly IConnector connector;
    private readonly AircraftProfileManager profileManager;
    private readonly Stopwatch stopwatch = new();

    private long? startMilliseconds;
    private long? endMilliseconds;
    private SimStateStruct? startState;

    public List<(long milliseconds, AircraftPositionStruct position)> Records { get; private set; } = new();
    public List<CustomAircraftVariablesStruct?> CustomRecords { get; private set; } = new();

    public AircraftProfile? CurrentProfile { get; private set; }
    public string? AircraftTitle { get; set; }

    private int currentFrame;

    private double rate = 1;
    private bool repeat = false;
    private int? pausedFrame;
    private double? pausedRate;
    private bool isReplayStopping;
    private long? replayMilliseconds;
    private long? pausedMilliseconds;
    private long offsetStartMilliseconds = 0;
    private bool forceReset = false;

    private AircraftPositionStruct? currentPosition = null;
    private long? lastTriggeredMilliseconds = null;
    private TaskCompletionSource<bool>? tcs;

    public bool IsReplayable => Records.Count > 0;
    private bool IsReplaying => replayMilliseconds != null && pausedMilliseconds == null;
    private bool IsPausing => pausedMilliseconds != null;

    private bool IsAI([NotNullWhen(true)] string? aircraftTitle) => !string.IsNullOrEmpty(aircraftTitle);
    private uint? aiRequestId = null;
    private uint? aiId = null;

    public ReplayLogic(ILogger<ReplayLogic> logger, IConnector connector, AircraftProfileManager? profileManager = null)
    {
        logger.LogDebug("Creating instance of {class}", nameof(ReplayLogic));

        this.logger = logger;
        this.connector = connector;
        this.profileManager = profileManager ?? new AircraftProfileManager();

        RegisterEvents();
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
            DeregisterEvents();
        }
    }

    private void RegisterEvents()
    {
        connector.AircraftIdReceived += Connector_AircraftIdReceived;
        connector.CreatingObjectFailed += Connector_CreatingObjectFailed;
        connector.Frame += Connector_Frame;
    }

    private void DeregisterEvents()
    {
        connector.AircraftIdReceived -= Connector_AircraftIdReceived;
        connector.CreatingObjectFailed -= Connector_CreatingObjectFailed;
        connector.Frame -= Connector_Frame;
    }

    #region Public Functions

    public bool Replay()
    {
        if (Records.Count == 0)
        {
            logger.LogError("Cannot replay with no data!");
            return false;
        }

        if (IsReplaying)
        {
            logger.LogError("Cannot replay while replaying!");
            return false;
        }

        if (IsPausing)
        {
            return ResumeReplay();
        }

        if (isReplayStopping)
        {
            logger.LogError("Cannot replay while stopping replay!");
            return false;
        }

        logger.LogInformation("Start replaying...");

        if (IsAI(AircraftTitle))
        {
            if (aiRequestId != null)
            {
                logger.LogError("Cannot replay when AI request is unfulfilled!");
                return false;
            }

            if (aiId != null)
            {
                logger.LogError("Cannot replay when AI ID is still set!");
                return false;
            }

            aiRequestId = connector.Spawn(AircraftTitle, Records.First().position);
        }
        else
        {
            // Freeze user aircraft immediately to prevent physics stall/drop
            connector.Freeze(0);
        }

        var initFrame = (currentFrame >= 0 && currentFrame < Records.Count) ? currentFrame : 0;
        currentFrame = initFrame;
        var (recMs, recPos) = Records[initFrame];
        var initCustom = initFrame < CustomRecords.Count ? CustomRecords[initFrame] : null;

        MoveAircraft(recMs - startMilliseconds!.Value, recPos, null, null, recMs - startMilliseconds!.Value, initCustom, null);

        _ = Task.Run(async () =>
        {
            try
            {
                await VpilotService.DisconnectIfConnectedAsync(logger);
            }
            catch { }
        });

        stopwatch.Restart();
        var initialElapsed = (long)((recMs - startMilliseconds.Value) / rate);
        replayMilliseconds = stopwatch.ElapsedMilliseconds - initialElapsed;
        RunReplay();

        return true;
    }

    public bool PauseReplay()
    {
        if (!IsReplaying)
        {
            logger.LogError("Cannot pause replay when not replaying!");
            return false;
        }

        logger.LogInformation("Pause replay at frame {currentFrame}.", currentFrame);

        pausedMilliseconds = stopwatch.ElapsedMilliseconds;
        pausedFrame = currentFrame;
        pausedRate = rate;

        return true;
    }

    public bool ResumeReplay()
    {
        if (!IsPausing)
        {
            logger.LogError("Cannot resume replay when not pausing!");
            return false;
        }

        logger.LogInformation("Resume replay from frame {currentFrame}.", pausedFrame);

        _ = Task.Run(async () =>
        {
            try
            {
                await VpilotService.DisconnectIfConnectedAsync(logger);
            }
            catch { }
        });

        if (pausedMilliseconds.HasValue && replayMilliseconds.HasValue)
        {
            replayMilliseconds = stopwatch.ElapsedMilliseconds - (long)((pausedMilliseconds.Value - replayMilliseconds.Value) * (pausedRate ?? 1) / rate);
            pausedMilliseconds = null;
        }

        return true;
    }

    public bool StopReplay()
    {
        if (isReplayStopping)
        {
            logger.LogError("Cannot stop replay when replay is already stopping!");
            return false;
        }

        logger.LogInformation("Stop replay.");

        isReplayStopping = true;
        tcs?.TrySetResult(true);

        return true;
    }

    public void Seek(int value)
    {
        logger.LogInformation("Seek to frame {value}", value);

        if (value < 0 || value >= Records.Count) return;

        currentFrame = value;
        pausedFrame = currentFrame;

        var (recordedMilliseconds, position) = Records[value];
        var recordedElapsed = recordedMilliseconds - startMilliseconds!.Value;

        var custom = (value < CustomRecords.Count) ? CustomRecords[value] : null;

        // Position aircraft in MSFS directly at target frame coordinates and altitude
        MoveAircraft(recordedElapsed, position, null, null, recordedElapsed, custom, null);
        CurrentFrameChanged?.Invoke(this, new(currentFrame));

        if (replayMilliseconds.HasValue)
        {
            var newElapsed = (long)(recordedElapsed / rate);
            replayMilliseconds = stopwatch.ElapsedMilliseconds - newElapsed;
            if (pausedMilliseconds.HasValue)
            {
                pausedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }
    }

    public void TrimStart()
    {
        logger.LogInformation("Trim start from frame {frame}", currentFrame);
        var trimFrame = currentFrame;

        if (IsPausing)
        {
            pausedMilliseconds = replayMilliseconds;
            pausedFrame = 0;
            forceReset = true;
        }
        else
        {
            offsetStartMilliseconds = 0;
        }

        (var currentElapsed, _) = Records[trimFrame];
        startMilliseconds = currentElapsed;
        currentFrame = 0;
        CurrentFrameChanged?.Invoke(this, new(currentFrame));
        Records = Records.Skip(trimFrame).ToList();
        CustomRecords = CustomRecords.Skip(trimFrame).ToList();
        RecordsUpdated?.Invoke(this, new(null, startState?.AircraftTitle, Records.Count));
    }

    public void TrimEnd()
    {
        logger.LogDebug("Trim end from frame {frame}", currentFrame);
        var trimFrame = currentFrame;

        if (!IsPausing)
        {
            offsetStartMilliseconds = 0;
            currentFrame = 0;
            CurrentFrameChanged?.Invoke(this, new(currentFrame));
        }

        Records = Records.Take(trimFrame + 1).ToList();
        CustomRecords = CustomRecords.Take(trimFrame + 1).ToList();
        RecordsUpdated?.Invoke(this, new(null, startState?.AircraftTitle, Records.Count));
    }

    public void ChangeRate(double rate)
    {
        this.rate = rate;
    }

    public void SetRepeat(bool repeat)
    {
        this.repeat = repeat;
    }

    public void Unfreeze()
    {
        if (replayMilliseconds != null)
        {
            if (!IsAI(AircraftTitle))
            {
                connector.Unfreeze(0);
            }
            else if (aiId.HasValue)
            {
                connector.Unfreeze(aiId.Value);
            }
        }
    }

    public void NotifyPosition(AircraftPositionStruct? value)
    {
        currentPosition = value;
    }

    public void FromData(string? fileName, SavedData data)
    {
        startMilliseconds = data.StartTime;
        endMilliseconds = data.EndTime;
        startState = data.StartState == null ? null : SimState.ToStruct(data.StartState);
        Reset();
        Records = data.Records.Select(r => (r.Time, AircraftPosition.ToStruct(r.Position))).ToList();
        CustomRecords = data.Records.Select(r => r.CustomVariables != null ? (CustomAircraftVariablesStruct?)CustomAircraftVariablesStruct.FromArray(r.CustomVariables) : null).ToList();

        if (!string.IsNullOrWhiteSpace(data.ProfileId))
        {
            CurrentProfile = profileManager.GetProfileById(data.ProfileId);
        }
        else if (data.StartState != null)
        {
            CurrentProfile = profileManager.FindProfileByTitle(data.StartState.AircraftTitle);
        }
        else
        {
            CurrentProfile = null;
        }

        connector.SetActiveProfile(CurrentProfile);
        ProfileChanged?.Invoke(this, CurrentProfile);

        RecordsUpdated?.Invoke(this, new(fileName, data.StartState?.AircraftTitle, Records.Count));
        CurrentFrameChanged?.Invoke(this, new(currentFrame));
    }

    public SavedData ToData(string clientVersion)
    {
        if (startMilliseconds == null) throw new InvalidOperationException("Invalid replay data without start time!");
        if (endMilliseconds == null) throw new InvalidOperationException("Invalid replay data without end time!");

        var combined = Records.Select((r, i) => (
            r.milliseconds,
            r.position,
            i < CustomRecords.Count ? CustomRecords[i] : null
        )).ToList();

        return new(
            clientVersion,
            startMilliseconds.Value,
            endMilliseconds.Value,
            startState,
            combined,
            CurrentProfile?.Id,
            CurrentProfile?.Variables.Count ?? 0
        );
    }

    #endregion

    #region Private Functions

    private void Connector_AircraftIdReceived(object? sender, AircraftIdReceivedEventArgs e)
    {
        if (IsAI(AircraftTitle) && aiRequestId == e.RequestId && aiId == null)
        {
            logger.LogDebug("Set AI ID {objectID}", e.ObjectId);
            aiRequestId = null;
            aiId = e.ObjectId;
        }
    }

    private void Connector_CreatingObjectFailed(object? sender, EventArgs e)
    {
        if (IsAI(AircraftTitle) && aiRequestId != null)
        {
            logger.LogDebug("Fail to spawn for request {requestID}", aiRequestId);
            aiRequestId = null;
            StopReplay();
        }
    }

    private void Connector_Frame(object? sender, EventArgs e)
    {
        Tick();
    }

    private async Task RunReplay()
    {
        if (!IsAI(AircraftTitle))
        {
            connector.Freeze(0);
        }

        while (true)
        {
            tcs = new TaskCompletionSource<bool>();
            await tcs.Task;
            tcs = null;

            if (isReplayStopping)
            {
                FinishReplay(false);
                return;
            }

            var replayStartTime = replayMilliseconds;
            if (replayStartTime == null)
            {
                continue;
            }

            if (IsAI(AircraftTitle) && aiId == null)
            {
                continue;
            }

            if (IsPausing)
            {
                continue;
            }

            if (Records.Count == 0)
            {
                FinishReplay(false);
                return;
            }

            var currentElapsed = (long)((stopwatch.ElapsedMilliseconds - replayStartTime.Value) * rate);

            if (currentFrame < 0) currentFrame = 0;

            // Sync currentFrame to currentElapsed: smoothly move forward or backward as needed
            while (currentFrame < Records.Count - 1 && currentElapsed > (Records[currentFrame].milliseconds - startMilliseconds!.Value))
            {
                currentFrame++;
            }
            while (currentFrame > 0 && currentElapsed < (Records[currentFrame - 1].milliseconds - startMilliseconds!.Value))
            {
                currentFrame--;
            }

            // Check if replay reached the end
            if (currentFrame >= Records.Count - 1 && currentElapsed >= (Records[Records.Count - 1].milliseconds - startMilliseconds!.Value))
            {
                FinishReplay(true);
                return;
            }

            var (recordedMilliseconds, recordedPosition) = Records[currentFrame];
            var recordedElapsed = recordedMilliseconds - startMilliseconds!.Value;

            long? lastElapsed = null;
            AircraftPositionStruct? lastPosition = null;
            CustomAircraftVariablesStruct? lastCustom = null;

            if (currentFrame > 0)
            {
                var (lastMs, lastPos) = Records[currentFrame - 1];
                lastElapsed = lastMs - startMilliseconds!.Value;
                lastPosition = lastPos;
                lastCustom = (currentFrame - 1 < CustomRecords.Count) ? CustomRecords[currentFrame - 1] : null;
            }

            var custom = (currentFrame < CustomRecords.Count) ? CustomRecords[currentFrame] : null;

            MoveAircraft(recordedElapsed, recordedPosition, lastElapsed, lastPosition, currentElapsed, custom, lastCustom);

            CurrentFrameChanged?.Invoke(this, new(currentFrame));
        }
    }

    private void FinishReplay(bool reachedLastFrame)
    {
        logger.LogInformation("Replay finished.");

        isReplayStopping = false;

        if (IsAI(AircraftTitle))
        {
            if (aiId.HasValue)
            {
                connector.Despawn(aiId.Value);
                aiId = null;
            }
        }
        else
        {
            Unfreeze();
        }

        Reset();

        if (reachedLastFrame && repeat)
        {
            Replay();
        }
        else
        {
            ReplayFinished?.Invoke(this, new EventArgs());
        }
    }

    private void Reset()
    {
        pausedMilliseconds = null;
        pausedFrame = null;
        replayMilliseconds = null;
        currentFrame = 0;
        offsetStartMilliseconds = 0;
    }

    private void MoveAircraft(long nextElapsed, AircraftPositionStruct position, long? lastElapsed, AircraftPositionStruct? lastPosition, long currentElapsed, CustomAircraftVariablesStruct? customData, CustomAircraftVariablesStruct? lastCustomData)
    {
        logger.LogTrace("Delta time {delta} {current} {recorded}.", currentElapsed - nextElapsed, currentElapsed, nextElapsed);

        var nextValue = AircraftPositionStructOperator.ToSet(position);
        double interpolation = 1.0;
        if (lastPosition.HasValue && lastElapsed.HasValue && nextElapsed > lastElapsed.Value)
        {
            interpolation = (double)(currentElapsed - lastElapsed.Value) / (nextElapsed - lastElapsed.Value);
            interpolation = Math.Clamp(interpolation, 0.0, 1.0);
            if (interpolation == 0.5)
            {
                interpolation = 0.501;
            }
            nextValue = AircraftPositionStructOperator.Interpolate(nextValue, AircraftPositionStructOperator.ToSet(lastPosition.Value), interpolation);
        }
        if (!IsAI(AircraftTitle) && currentPosition.HasValue && (lastTriggeredMilliseconds == null || stopwatch.ElapsedMilliseconds > lastTriggeredMilliseconds + EventThrottleMilliseconds))
        {
            lastTriggeredMilliseconds = stopwatch.ElapsedMilliseconds;
            connector.TriggerEvents(currentPosition.Value, position);
        }

        connector.Set(aiId ?? 0, nextValue);

        if (customData.HasValue && CurrentProfile != null)
        {
            var interpolatedCustom = customData.Value;
            if (lastCustomData.HasValue && lastElapsed.HasValue && nextElapsed > lastElapsed.Value)
            {
                var factor = Math.Clamp(interpolation, 0.0, 1.0);
                for (int i = 0; i < CurrentProfile.Variables.Count && i < CustomAircraftVariablesStruct.MaxVariables; i++)
                {
                    var v = CurrentProfile.Variables[i];
                    if (v.Interpolate)
                    {
                        interpolatedCustom[i] = lastCustomData.Value[i] + (customData.Value[i] - lastCustomData.Value[i]) * factor;
                    }
                    else
                    {
                        interpolatedCustom[i] = factor < 0.5 ? lastCustomData.Value[i] : customData.Value[i];
                    }
                }
            }
            connector.SetCustomVariables(aiId ?? 0, interpolatedCustom);
        }
    }

    private void Tick()
    {
        if (IsReplaying || IsPausing)
        {
            try
            {
                tcs?.SetResult(true);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogDebug(ex, "Cannot set TCS result on tick");
            }
        }
    }

    #endregion
}
