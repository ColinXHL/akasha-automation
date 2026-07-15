using AkashaAutomation.Core.Abstractions;
using AkashaAutomation.Core.Capture;
using AkashaAutomation.Core.Input;
using AkashaAutomation.Core.Ocr;
using AkashaAutomation.Core.Scheduling;
using AkashaAutomation.BetterGiPort.Compatibility.AutoPick;
using AkashaAutomation.Features.AutoPick;
using AkashaAutomation.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AkashaAutomation.Worker.IntegrationTests;

public sealed class SafeCoreRegistrationTests
{
    [Fact]
    public async Task AddSafeAutomationCore_RegistersCaptureButNeverRealInput()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSafeAutomationCore();
        await using var provider = services.BuildServiceProvider();

        var input = provider.GetRequiredService<IInputService>();
        var capture = provider.GetRequiredService<ICaptureSource>();

        Assert.IsType<DisabledInputService>(input);
        Assert.IsNotType<WindowsSendInputService>(input);
        Assert.IsType<WindowsGraphicsCaptureSource>(capture);
        Assert.IsType<InputArbiter>(provider.GetRequiredService<IInputArbiter>());
        var runtimeResources = provider.GetServices<IWorkerRuntimeResource>().ToArray();
        Assert.Contains(runtimeResources, resource => resource is AutomationInputRuntimeResource);
        Assert.Contains(runtimeResources, resource => resource is AutomationSchedulerHostedService);
        Assert.IsType<PaddleOcrEngine>(provider.GetRequiredService<IOcrEngine>());
        Assert.IsType<BetterGiAutoPickRecognizer>(provider.GetRequiredService<BetterGiAutoPickRecognizer>());
        Assert.IsType<AutoPickController>(provider.GetRequiredService<IAutoPickController>());
        Assert.IsType<AutoPickFeature>(provider.GetRequiredService<IAutomationFeature>());
        Assert.IsType<SingleFrameScheduler>(provider.GetRequiredService<SingleFrameScheduler>());
        Assert.Contains(
            provider.GetServices<IHostedService>(),
            service => service is AutomationSchedulerHostedService);
    }
}
