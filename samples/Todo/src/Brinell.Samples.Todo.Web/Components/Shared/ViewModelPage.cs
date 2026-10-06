using System.ComponentModel;
using Brinell.Samples.Shared.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Brinell.Samples.Todo.Web.Components.Shared;

/// <summary>
/// A page over a view model: it re-renders when the view model changes, tells it when the page
/// appears and goes away, and publishes the page's busy signal.
/// </summary>
/// <remarks>
/// <para>
/// What a MAUI <c>ContentPage</c> gets from binding and <c>OnAppearing</c> /
/// <c>OnDisappearing</c>. Changes can come from another thread (sync, the network-state file),
/// so every re-render goes through <see cref="ComponentBase.InvokeAsync(Action)"/>.
/// </para>
/// <para>
/// <b>The busy signal.</b> <c>&lt;body data-busy&gt;</c> is <c>true</c> while the view model
/// is busy (a command is running). It is written after each render in which it changed.
/// </para>
/// </remarks>
/// <typeparam name="TViewModel">The page's view model.</typeparam>
public abstract class ViewModelPage<TViewModel> : ComponentBase, IDisposable
    where TViewModel : ParentViewModel
{
    private bool? _publishedBusy;

    /// <summary>The page's view model.</summary>
    protected abstract TViewModel ViewModel { get; }

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    /// <inheritdoc />
    public virtual void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelChanged;
        ViewModel.OnViewDisappearing();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnViewModelChanged;
        ViewModel.OnViewAppearing();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var busy = ViewModel.IsBusy;
        if (_publishedBusy != busy)
        {
            _publishedBusy = busy;
            await JS.InvokeVoidAsync("brinellTodo.setBusy", busy);
        }
    }

    /// <summary>Re-renders the page, from whatever thread the change came on.</summary>
    protected void Refresh() => _ = InvokeAsync(StateHasChanged);

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Refresh();
}
