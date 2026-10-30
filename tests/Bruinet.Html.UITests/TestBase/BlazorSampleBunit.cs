using Bruinet.Html.Bunit;
using Bruinet.Samples.Blazor.App.Services;

namespace Bruinet.Html.UITests.TestBase;

/// <summary>The sample on the bUnit head (AD-030): its <c>Routes</c> and its four services.</summary>
internal static class BlazorSampleBunit
{
    public const string BaseUrl = "http://localhost";

    public static bool IsHead => Core.Testing.WebHeads.IsBunit;

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
