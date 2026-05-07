using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class ControlsManagerViewModel : ViewModelBase
{
    private readonly IControlRegistry _controlRegistry;
    private readonly ControlGenerationService _controlGenerationService;
    private readonly ILogger<ControlsManagerViewModel> _logger;
    private GeneratedControl? _selectedControl;
    private string _codePreview = "";

    public ControlsManagerViewModel(
        IControlRegistry controlRegistry,
        ControlGenerationService controlGenerationService,
        ILogger<ControlsManagerViewModel> logger)
    {
        _controlRegistry = controlRegistry;
        _controlGenerationService = controlGenerationService;
        _logger = logger;

        GeneratePendingCommand = new AsyncRelayCommand(GeneratePendingAsync);
        RegenerateCommand = new AsyncRelayCommand(RegenerateAsync, () => SelectedControl is not null);
    }

    public ObservableCollection<GeneratedControl> Controls { get; } = [];

    public GeneratedControl? SelectedControl
    {
        get => _selectedControl;
        set
        {
            if (SetProperty(ref _selectedControl, value))
                CodePreview = value?.Code ?? "";
        }
    }

    public string CodePreview
    {
        get => _codePreview;
        private set => SetProperty(ref _codePreview, value);
    }

    public ICommand GeneratePendingCommand { get; }
    public ICommand RegenerateCommand { get; }

    public void LoadControls()
    {
        Controls.Clear();
        foreach (var ctrl in _controlRegistry.GetAllControls())
            Controls.Add(ctrl);
    }

    private Task GeneratePendingAsync(CancellationToken ct)
    {
        _logger.LogInformation("Generate pending requested (requires approved proposals)");
        return Task.CompletedTask;
    }

    private Task RegenerateAsync(CancellationToken ct)
    {
        if (SelectedControl is not null)
            _logger.LogInformation("Regenerate requested for {ControlName}", SelectedControl.Name);
        return Task.CompletedTask;
    }
}
