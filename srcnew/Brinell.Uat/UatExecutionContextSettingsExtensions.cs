using Brinell.Core.Settings;

namespace Brinell.Uat;

public static class UatExecutionContextSettingsExtensions
{
    private const string SettingsItemKey = "Brinell.Uat.Settings";
    private const string TypedSettingsItemKeyPrefix = "Brinell.Uat.Settings.Typed.";

    public static void SetSettings(this UatExecutionContext context, TestSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        context.Items[SettingsItemKey] = settings;
    }

    public static TestSettings GetSettings(this UatExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(SettingsItemKey, out var value) &&
               value is TestSettings settings
            ? settings
            : TestSettings.Empty;
    }

    public static object GetSettings(this UatExecutionContext context, Type settingsType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settingsType);

        if (settingsType == typeof(TestSettings))
        {
            return context.GetSettings();
        }

        var cacheKey = TypedSettingsItemKeyPrefix + settingsType.AssemblyQualifiedName;
        if (context.Items.TryGetValue(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var settings = context.GetSettings().Bind(settingsType);
        context.Items[cacheKey] = settings;
        return settings;
    }
}
