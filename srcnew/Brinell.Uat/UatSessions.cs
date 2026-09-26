namespace Brinell.Uat;

/// <summary>
/// A fixture that runs several apps in one scenario: two devices on one backend, two users. Each
/// app is a session with its own test context and page objects; <c>{app} uses the app</c> makes
/// one current. See <c>.my/html/todo-blazor.md</c> section 4.
/// </summary>
/// <remarks>
/// The sessions share the suite's page and phrase types: a session changes which app a page object
/// drives, not which page objects there are. The app names are the <c>Apps</c> table's, which the
/// engine checks before it asks the fixture.
/// </remarks>
public interface IUatSessionHost
{
    /// <summary>
    /// Makes <paramref name="name"/> the current app, starting it when it is not running yet.
    /// </summary>
    /// <param name="name">The app's name, as in the <c>Apps</c> table.</param>
    /// <returns>The services the app's page objects and phrases resolve through, for the rest of the scenario.</returns>
    IServiceProvider UseSession(string name);
}

internal static class UatSessionSteps
{
    public const string CommandId = "Builtin.Session.Use";

    public const string Phrase = "{app} uses the app";

    /// <summary>Registers <c>{app} uses the app</c>, as a Given and a When, for a fixture that hosts sessions.</summary>
    public static void Register(UatCommandCatalog catalog, IUatSessionHost host, UatConfig config)
    {
        UatCommandHandler handler = (context, invocation, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requested = invocation.Arguments["app"];
            var app = config.Apps.FirstOrDefault(candidate => string.Equals(candidate.Name, requested, StringComparison.OrdinalIgnoreCase));
            if (config.Apps.Count > 0 && app is null)
            {
                return Task.FromResult(UatStepResult.Failed(
                    invocation,
                    $"No app '{requested}' in the Apps table of uat.config.md. It has: {string.Join(", ", config.Apps.Select(candidate => $"'{candidate.Name}'"))}."));
            }

            var name = app?.Name ?? requested;
            UatRuntime.ConfigureScope(context, host.UseSession(name));
            context.CurrentSession = name;
            context.Diagnostics.Add($"session:{name}");
            return Task.FromResult(UatStepResult.Passed(invocation));
        };

        catalog.Register(UatEffectiveStepKeyword.Given, Phrase, CommandId, allowsTable: false, handler: handler);
        catalog.Register(UatEffectiveStepKeyword.When, Phrase, CommandId, allowsTable: false, handler: handler);
    }
}
