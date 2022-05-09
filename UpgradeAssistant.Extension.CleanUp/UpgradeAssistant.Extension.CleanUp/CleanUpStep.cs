using Microsoft.Build.Construction;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.Extensions.Logging;

namespace UpgradeAssistant.Extension.CleanUp;

public class CleanUpStep : UpgradeStep
{
    public CleanUpStep(ILogger<CleanUpStep> logger) : base(logger)
    {
    }

    public override string Title => "Clean up step";
    public override string Description => "Cleans up NuGet references";
    private const string NugetReference = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers";

    protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        => Task.FromResult(true);

    protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return Task.FromResult(
            new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, Title, BuildBreakRisk.None));
    }

    protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Iterate through all projects in solution
        foreach (var project in context.Projects)
        {
            var projectFile = project.GetFile();

            var projectRoot = ProjectRootElement.Open(projectFile.FilePath);

            if (projectRoot is null)
            {
                throw new NullReferenceException(nameof(projectRoot));
            }

            var packageReferences = projectRoot.Items
                .Where(i => i.ItemType.Equals("PackageReference", StringComparison.OrdinalIgnoreCase));

            var packageToRemove = packageReferences
                .FirstOrDefault(p => NugetReference.Equals(p.Include));

            if (packageToRemove != null)
            {
                RemoveElement(packageToRemove);
                projectRoot.Save();
                Logger.LogInformation($"Removed package {packageToRemove.Include}");
            }
        }

        return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, Title));
    }

    // Code taken from UpgradeAssistant source code
    // https://github.com/dotnet/upgrade-assistant/blob/main/src/components/Microsoft.DotNet.UpgradeAssistant.MSBuild/MSBuildExtensions.cs
    private static void RemoveElement(ProjectElement element)
    {
        var itemGroup = element.Parent;
        itemGroup.RemoveChild(element);

        if (!itemGroup.Children.Any())
        {
            // If no element remain in the item group, remove it
            itemGroup.Parent.RemoveChild(itemGroup);
        }
    }
}