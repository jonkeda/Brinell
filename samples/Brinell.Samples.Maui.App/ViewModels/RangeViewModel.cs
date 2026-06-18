namespace Brinell.Samples.Maui.App.ViewModels;

public class RangeViewModel : ParentViewModel
{
    private double sliderValue = 50;
    private double sliderMinimum = 0;
    private double sliderMaximum = 100;

    private double stepperValue = 5;
    private double stepperMinimum = 0;
    private double stepperMaximum = 10;
    private double stepperIncrement = 1;

    private string statusMessage = "Ready. Adjust the Slider or Stepper to test.";

    public double SliderValue
    {
        get => sliderValue;
        set
        {
            if (sliderValue != value)
            {
                sliderValue = value;
                OnPropertyChanged();
                UpdateSliderStatus();
            }
        }
    }

    public double SliderMinimum
    {
        get => sliderMinimum;
        set => SetProperty(ref sliderMinimum, value);
    }

    public double SliderMaximum
    {
        get => sliderMaximum;
        set => SetProperty(ref sliderMaximum, value);
    }

    public double StepperValue
    {
        get => stepperValue;
        set
        {
            if (stepperValue != value)
            {
                stepperValue = value;
                OnPropertyChanged();
                UpdateStepperStatus();
            }
        }
    }

    public double StepperMinimum
    {
        get => stepperMinimum;
        set => SetProperty(ref stepperMinimum, value);
    }

    public double StepperMaximum
    {
        get => stepperMaximum;
        set => SetProperty(ref stepperMaximum, value);
    }

    public double StepperIncrement
    {
        get => stepperIncrement;
        set => SetProperty(ref stepperIncrement, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public ICommand ResetCommand => new RelayCommand(Reset);

    private void UpdateSliderStatus()
    {
        var percentage = ((SliderValue - SliderMinimum) / (SliderMaximum - SliderMinimum)) * 100;
        StatusMessage = $"✓ Slider value: {SliderValue:F1} ({percentage:F0}% of range)";
    }

    private void UpdateStepperStatus()
    {
        var stepsFromMin = (StepperValue - StepperMinimum) / StepperIncrement;
        var totalSteps = (StepperMaximum - StepperMinimum) / StepperIncrement;
        StatusMessage = $"✓ Stepper value: {StepperValue:F0} (step {stepsFromMin:F0} of {totalSteps:F0})";
    }

    private void Reset()
    {
        SliderValue = 50;
        StepperValue = 5;
        StatusMessage = "Ready. Adjust the Slider or Stepper to test.";
    }
}
