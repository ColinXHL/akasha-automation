using AkashaAutomation.Worker.Bridge;
using AkashaAutomation.Worker.Configuration;

namespace AkashaAutomation.Worker.Hosting;

public sealed class WorkerStatusProvider(
    WorkerStateMachine stateMachine,
    EmergencyStopController emergencyStop)
{
    private readonly object _errorGate = new();
    private WorkerErrorStatus? _lastError;

    public WorkerStatus GetStatus(
        WorkerLaunchOptions options,
        string workerVersion,
        DateTimeOffset startedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerVersion);

        var emergency = emergencyStop.Snapshot;
        WorkerErrorStatus? lastError;
        lock (_errorGate)
        {
            lastError = _lastError;
        }

        return new WorkerStatus(
            ToProtocolState(stateMachine.State),
            CompanionProtocol.CurrentVersion,
            workerVersion,
            options.ParentProcessId,
            startedAtUtc,
            false,
            new EmergencyStopStatus(
                emergency.IsActive,
                emergency.Reason,
                emergency.TriggeredAtUtc),
            new GameWindowStatus("not_found", false, null, null),
            new SubsystemStatus("not_started", false),
            new SubsystemStatus("not_started", false),
            new FeatureStatuses(
                new FeatureStatus(false, false),
                new FeatureStatus(false, false)),
            lastError);
    }

    public void ReportError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        lock (_errorGate)
        {
            _lastError = new WorkerErrorStatus(code, message, DateTimeOffset.UtcNow);
        }
    }

    private static string ToProtocolState(WorkerState state) =>
        state switch
        {
            WorkerState.Created => "created",
            WorkerState.Connecting => "connecting",
            WorkerState.Handshaking => "handshaking",
            WorkerState.Ready => "ready",
            WorkerState.Running => "running",
            WorkerState.Stopping => "stopping",
            WorkerState.Stopped => "stopped",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };
}
