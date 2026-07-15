using Microsoft.Extensions.Logging;
using AkashaAutomation.Features.AutoPick;

namespace AkashaAutomation.Worker.Hosting;

public sealed class WorkerRuntime
{
    public WorkerRuntime(
        IEnumerable<IWorkerRuntimeResource>? resources = null,
        ILoggerFactory? loggerFactory = null,
        int commandQueueCapacity = WorkerCommandQueue.DefaultCapacity,
        Func<WorkerStatusProvider, EmergencyStopController, IWorkerCommandHandler>? commandHandlerFactory = null,
        IAutoPickController? autoPickController = null)
    {
        StateMachine = new WorkerStateMachine();
        EmergencyStop = new EmergencyStopController();
        StatusProvider = new WorkerStatusProvider(StateMachine, EmergencyStop, autoPickController);
        CommandHandler = commandHandlerFactory?.Invoke(StatusProvider, EmergencyStop)
                         ?? new WorkerCommandHandler(StatusProvider, EmergencyStop, autoPickController);
        CommandQueue = new WorkerCommandQueue(
            CommandHandler,
            commandQueueCapacity,
            EmergencyStop.CancellationToken);
        Shutdown = new WorkerShutdownCoordinator(
            EmergencyStop,
            StateMachine,
            CommandQueue,
            StatusProvider,
            resources,
            loggerFactory?.CreateLogger<WorkerShutdownCoordinator>());
    }

    public WorkerStateMachine StateMachine { get; }

    public EmergencyStopController EmergencyStop { get; }

    public WorkerStatusProvider StatusProvider { get; }

    public IWorkerCommandHandler CommandHandler { get; }

    public WorkerCommandQueue CommandQueue { get; }

    public WorkerShutdownCoordinator Shutdown { get; }
}
