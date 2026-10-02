using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// Sends a prompt into Copilot Chat's open conversation through Visual Studio's Copilot brokered service
/// (<c>Microsoft.VisualStudio.Copilot</c> 0.3): the active chat session's <c>ICopilotSession.SendRequestAsync</c>.
/// (<c>ICopilotService.SendRequestAsync</c> needs the Copilot backend mode, which is off by default.) Its contracts ship
/// with Visual Studio only, not as a package, so they are reached by reflection; any step that is missing
/// in this Visual Studio makes it answer false, and the caller puts the prompt on the clipboard instead.
/// </summary>
internal static class CopilotChatSender
{
    private const string Contracts = "Microsoft.VisualStudio.Copilot";
    private const string Namespace = "Microsoft.VisualStudio.Copilot.";

    // How long a send may take to fail before it counts as sent: the request runs as long as the agent does.
    private static readonly TimeSpan Accepted = TimeSpan.FromSeconds(3);

    /// <summary>Sends <paramref name="prompt"/> to the active chat session; false when it could not.</summary>
    /// <param name="log">Says each step, and why it stopped.</param>
    public static async Task<bool> TrySendAsync(AsyncPackage package, string prompt, Action<string> log, CancellationToken cancellationToken)
    {
        try
        {
            return await SendAsync(package, prompt, log, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log($"Copilot Chat: failed: {(exception as TargetInvocationException)?.InnerException ?? exception}");
            return false;
        }
    }

    private static async Task<bool> SendAsync(AsyncPackage package, string prompt, Action<string> log, CancellationToken cancellationToken)
    {
        var contracts = LoadContracts();
        var serviceType = contracts?.GetType(Namespace + "ICopilotService");
        var sessionType = contracts?.GetType(Namespace + "ICopilotSession");
        var requestType = contracts?.GetType(Namespace + "CopilotRequest");
        var descriptor = contracts?.GetType(Namespace + "CopilotDescriptors")?.GetProperty("CopilotService")?.GetValue(null) as ServiceRpcDescriptor;
        log($"Copilot Chat: contracts {contracts?.Location ?? "not found"}; service {serviceType is not null}, session {sessionType is not null}, request {requestType is not null}, descriptor {descriptor is not null}.");
        if (serviceType is null || sessionType is null || requestType is null || descriptor is null)
        {
            return false;
        }

        if (await package.GetServiceAsync(typeof(SVsBrokeredServiceContainer)) is not IBrokeredServiceContainer container)
        {
            log("Copilot Chat: no brokered service container.");
            return false;
        }

        var broker = container.GetFullAccessServiceBroker();
        var getProxy = typeof(IServiceBroker).GetMethod(nameof(IServiceBroker.GetProxyAsync))!.MakeGenericMethod(serviceType);
        var proxy = await AwaitAsync(getProxy.Invoke(broker, [descriptor, default(ServiceActivationOptions), cancellationToken]));
        if (proxy is null)
        {
            log($"Copilot Chat: the broker has no {descriptor.Moniker}.");
            return false;
        }

        var sending = false;
        try
        {
            // The chat window was just opened: its session can take a moment to become the active one.
            object? session = null;
            for (var attempt = 0; attempt < 10 && session is null; attempt++)
            {
                session = await AwaitAsync(serviceType.GetMethod("GetActiveChatSessionAsync")!.Invoke(proxy, [cancellationToken]));
                if (session is null)
                {
                    await Task.Delay(200, cancellationToken);
                }
            }

            if (session is null)
            {
                log("Copilot Chat: no active chat session.");
                return false;
            }

            var info = await AwaitAsync(sessionType.GetMethod("GetSessionInfoAsync")!.Invoke(session, [cancellationToken]));
            var id = info?.GetType().GetProperty("Id")?.GetValue(info);
            log($"Copilot Chat: session {id}.");
            if (id is null)
            {
                return false;
            }

            // In the mode the chat window is in (Ask or Agent): the options are left at their defaults.
            var message = Activator.CreateInstance(requestType, prompt)!;
            var send = sessionType.GetMethods().Single(m => m.Name == "SendRequestAsync" && m.GetParameters().Length == 2);
            var request = AwaitAsync(send.Invoke(session, [message, cancellationToken]));
            if (await Task.WhenAny(request, Task.Delay(Accepted, cancellationToken)) == request)
            {
                // Answered at once: an error, or a short answer; either way the chat shows it.
                var response = await request;
                (proxy as IDisposable)?.Dispose();
                var status = response?.GetType().GetProperty("Status")?.GetValue(response)?.ToString();
                log($"Copilot Chat: answered at once: status {status ?? "none"}.");
                return status is "Success" or "Unknown";
            }

            // Still running: the chat has it. The proxy stays until the agent is done.
            log("Copilot Chat: sent; the request is running.");
            sending = true;
            _ = request.ContinueWith(
                done =>
                {
                    log(done.IsFaulted
                        ? $"Copilot Chat: the request failed: {done.Exception?.InnerException}"
                        : $"Copilot Chat: the request is done: status {done.Result?.GetType().GetProperty("Status")?.GetValue(done.Result)}.");

                    (proxy as IDisposable)?.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return true;
        }
        finally
        {
            if (!sending)
            {
                (proxy as IDisposable)?.Dispose();
            }
        }
    }

    // The contracts assembly as Visual Studio loads it, else from its folder beside devenv.exe.
    private static Assembly? LoadContracts()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == Contracts && a.GetType(Namespace + "ICopilotService") is not null);
        if (loaded is not null)
        {
            return loaded;
        }

        var ide = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
        var path = Path.Combine(ide, "CommonExtensions", "Microsoft", "Microsoft.VisualStudio.Copilot.Contracts", Contracts + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }

    // Awaits a Task, Task<T> or ValueTask<T> returned by a method called by reflection, and gives its result.
    private static async Task<object?> AwaitAsync(object? awaitable)
    {
        if (awaitable is null)
        {
            return null;
        }

        var task = awaitable as Task ?? (Task)awaitable.GetType().GetMethod("AsTask")!.Invoke(awaitable, null)!;
        await task;
        return task.GetType().IsGenericType ? task.GetType().GetProperty("Result")!.GetValue(task) : null;
    }
}
