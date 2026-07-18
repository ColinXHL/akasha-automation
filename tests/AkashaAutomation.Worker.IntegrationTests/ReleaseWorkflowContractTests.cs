namespace AkashaAutomation.Worker.IntegrationTests;

public sealed class ReleaseWorkflowContractTests
{
    [Fact]
    public void PublishWorkflow_ShouldDispatchVerifiedPackageMetadata()
    {
        var workflow = File.ReadAllText(
            Path.Combine(GetRepositoryRoot(), ".github", "workflows", "publish.yml"));

        Assert.Contains("notify_navigator:", workflow, StringComparison.Ordinal);
        Assert.Contains("automation_plugin_released", workflow, StringComparison.Ordinal);
        Assert.Contains("AKASHA_NAVIGATOR_DISPATCH_TOKEN", workflow, StringComparison.Ordinal);
        Assert.Contains("needs.build.outputs.package_size", workflow, StringComparison.Ordinal);
        Assert.Contains("needs.build.outputs.package_sha256", workflow, StringComparison.Ordinal);
        Assert.Contains("needs.validate.outputs.draft == 'false'", workflow, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AkashaAutomation.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Akasha Automation repository root.");
    }
}
