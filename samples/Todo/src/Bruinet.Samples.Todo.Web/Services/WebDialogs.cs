using Bruinet.Samples.Todo.Core.Services;

namespace Bruinet.Samples.Todo.Web.Services;

/// <summary>One question on screen, waiting for the user's answer.</summary>
/// <param name="Title">The dialog's title.</param>
/// <param name="Message">What it asks or says.</param>
/// <param name="Accept">The accept button's text.</param>
/// <param name="Cancel">The cancel button's text; <c>null</c> for an alert with one button.</param>
public sealed record DialogRequest(string Title, string Message, string Accept, string? Cancel)
{
    internal TaskCompletionSource<bool> Answer { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// <see cref="IDialogs"/> for the web head: a view model's question becomes a
/// <see cref="Current"/> request, which the layout's dialog component shows and answers.
/// </summary>
/// <remarks>
/// A singleton, for the same reason as <see cref="WebNavigator"/>: this host is one device. One
/// dialog at a time, as in the MAUI app.
/// </remarks>
public sealed class WebDialogs : IDialogs
{
    /// <summary>The dialog on screen, or <c>null</c>.</summary>
    public DialogRequest? Current { get; private set; }

    /// <summary>Raised when <see cref="Current"/> changes.</summary>
    public event EventHandler? Changed;

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => Show(new DialogRequest(title, message, accept, cancel));

    /// <inheritdoc />
    public Task AlertAsync(string title, string message, string ok = "OK")
        => Show(new DialogRequest(title, message, ok, null));

    /// <summary>Answers the dialog on screen: <c>true</c> for accept.</summary>
    public void Answer(bool accepted)
    {
        if (Current is not { } request)
        {
            return;
        }

        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
        request.Answer.TrySetResult(accepted);
    }

    private Task<bool> Show(DialogRequest request)
    {
        if (Current is not null)
        {
            throw new InvalidOperationException($"A dialog is already open: \"{Current.Title}\".");
        }

        Current = request;
        Changed?.Invoke(this, EventArgs.Empty);
        return request.Answer.Task;
    }
}
