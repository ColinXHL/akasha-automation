using System.Text.Json;
using AkashaAutomation.Worker.Bridge;

namespace AkashaAutomation.Worker.Hosting;

public sealed class WorkerCommandHandler(
    WorkerStatusProvider statusProvider,
    EmergencyStopController emergencyStop) : IWorkerCommandHandler
{
    public ValueTask<CompanionEnvelope> HandleAsync(
        WorkerCommandContext command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = command.Request;

        if (request.Method!.Equals("worker.echo", StringComparison.Ordinal))
        {
            return ValueTask.FromResult(SuccessResponse(request, request.Payload?.Clone()));
        }

        if (request.Method.Equals("worker.getStatus", StringComparison.Ordinal))
        {
            var status = statusProvider.GetStatus(
                command.Options,
                command.WorkerVersion,
                command.StartedAtUtc);
            return ValueTask.FromResult(
                SuccessResponse(
                    request,
                    JsonSerializer.SerializeToElement(status, CompanionProtocol.JsonOptions)));
        }

        if (request.Method.Equals("worker.shutdown", StringComparison.Ordinal))
        {
            return ValueTask.FromResult(
                SuccessResponse(
                    request,
                    JsonSerializer.SerializeToElement(
                        new { accepted = true },
                        CompanionProtocol.JsonOptions)));
        }

        if (request.Method.Equals("automation.emergencyStop", StringComparison.Ordinal))
        {
            emergencyStop.Trigger(WorkerStopReason.CompanionEmergencyStop);
            return ValueTask.FromResult(
                SuccessResponse(
                    request,
                    JsonSerializer.SerializeToElement(
                        new { accepted = true, active = true },
                        CompanionProtocol.JsonOptions)));
        }

        return ValueTask.FromResult(
            new CompanionEnvelope
            {
                Type = CompanionProtocol.Response,
                CorrelationId = request.CorrelationId,
                Error = new CompanionError(
                    "method_not_found",
                    $"Unknown companion method '{request.Method}'."),
            });
    }

    private static CompanionEnvelope SuccessResponse(
        CompanionEnvelope request,
        JsonElement? payload) =>
        new()
        {
            Type = CompanionProtocol.Response,
            CorrelationId = request.CorrelationId,
            Payload = payload,
        };
}
