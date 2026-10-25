using Brinell.Html.Bunit;
using Brinell.Samples.Blazor.App.Services;

namespace Brinell.Blazor.UITests.TestBase;

/// <summary>The sample on the bUnit head (.my/bunit/plan.md step 1): its <c>Routes</c> and its four services.</summary>
internal static class BlazorSampleBunit
{
    public const string BaseUrl = "http://localhost";

    public static bool IsHead => string.Equals(Environment.GetEnvironmentVariable("BRINELL_HEAD"), "bunit", StringComparison.OrdinalIgnoreCase);

    public static BunitTestContext Create() => new(new BunitTestContextOptions
    {
        RootComponent = typeof(Samples.Blazor.App.Components.Routes),
        ConfigureServices = services =>
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<DataService>();
            services.AddSingleton<MediaService>();
            services.AddSingleton<ToastService>();
        },
    });
}
