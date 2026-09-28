using Brinell.Uat;

namespace Brinell.Presenter.Services;

internal sealed record BddTargetDescriptor(
    string Name,
    string AppPathEnvironmentVariable,
    bool SupportsPresenterAutPlacement);

internal static class BddTargetRegistry
{
    private static readonly BddTargetDescriptor[] Targets =
    [
        new("MAUI", "APPIUM_APP_PATH", true),
        new("WPF", "WPF_APP_PATH", false),
        new("WINFORMS", "WINFORMS_APP_PATH", false),
        new("BLAZOR", "BLAZOR_APP_PATH", false),
        new("HTML", "HTML_APP_PATH", false),
        new("STRIDE", "STRIDE_APP_PATH", false)
    ];

    public static IReadOnlyList<BddTargetDescriptor> SupportedTargets => Targets;

    public static string SupportedTargetList => string.Join(", ", Targets.Select(target => target.Name));

    public static bool TryGet(string? targetName, out BddTargetDescriptor descriptor)
    {
        descriptor = default!;
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return false;
        }

        var target = Targets.FirstOrDefault(candidate =>
            candidate.Name.Equals(targetName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return false;
        }

        descriptor = target;
        return true;
    }

    public static BddTargetDescriptor GetRequired(BddConfig config)
    {
        var targetName = config.Runtime.TryGetValue("Target", out var value) ? value : string.Empty;
        return TryGet(targetName, out var descriptor)
            ? descriptor
            : throw new InvalidOperationException(
                $"Runtime Target must be one of: {SupportedTargetList}.");
    }
}
