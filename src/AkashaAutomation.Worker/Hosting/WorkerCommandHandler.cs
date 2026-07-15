using System.Text.Json;
using AkashaAutomation.Worker.Bridge;
using AkashaAutomation.Features.AutoPick;

namespace AkashaAutomation.Worker.Hosting;

public sealed class WorkerCommandHandler(
    WorkerStatusProvider statusProvider,
    EmergencyStopController emergencyStop,
    IAutoPickController? autoPickController = null) : IWorkerCommandHandler
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

        if (request.Method.Equals("features.autoPick.getOptions", StringComparison.Ordinal))
        {
            return autoPickController is null
                ? ValueTask.FromResult(UnavailableResponse(request))
                : ValueTask.FromResult(SuccessResponse(
                    request,
                    JsonSerializer.SerializeToElement(autoPickController.Options, CompanionProtocol.JsonOptions)));
        }

        if (request.Method.Equals("features.autoPick.setEnabled", StringComparison.Ordinal))
        {
            if (autoPickController is null)
            {
                return ValueTask.FromResult(UnavailableResponse(request));
            }

            if (request.Payload is not { } enabledPayload ||
                !enabledPayload.TryGetProperty("enabled", out var enabledElement) ||
                enabledElement.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                return ValueTask.FromResult(InvalidPayloadResponse(request));
            }

            autoPickController.SetEnabled(enabledElement.GetBoolean());
            return ValueTask.FromResult(SuccessResponse(
                request,
                JsonSerializer.SerializeToElement(autoPickController.Options, CompanionProtocol.JsonOptions)));
        }

        if (request.Method.Equals("features.autoPick.setOptions", StringComparison.Ordinal))
        {
            if (autoPickController is null)
            {
                return ValueTask.FromResult(UnavailableResponse(request));
            }

            try
            {
                var options = request.Payload?.Deserialize<AutoPickOptions>(CompanionProtocol.JsonOptions);
                if (options is null)
                {
                    return ValueTask.FromResult(InvalidPayloadResponse(request));
                }

                autoPickController.SetOptions(options);
                return ValueTask.FromResult(SuccessResponse(
                    request,
                    JsonSerializer.SerializeToElement(autoPickController.Options, CompanionProtocol.JsonOptions)));
            }
            catch (JsonException)
            {
                return ValueTask.FromResult(InvalidPayloadResponse(request));
            }
            catch (ArgumentException)
            {
                return ValueTask.FromResult(InvalidPayloadResponse(request));
            }
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

    private static CompanionEnvelope InvalidPayloadResponse(CompanionEnvelope request) =>
        new()
        {
            Type = CompanionProtocol.Response,
            CorrelationId = request.CorrelationId,
            Error = new CompanionError("invalid_payload", "The AutoPick payload is invalid."),
        };

    private static CompanionEnvelope UnavailableResponse(CompanionEnvelope request) =>
        new()
        {
            Type = CompanionProtocol.Response,
            CorrelationId = request.CorrelationId,
            Error = new CompanionError("feature_unavailable", "AutoPick is not registered."),
        };
}
