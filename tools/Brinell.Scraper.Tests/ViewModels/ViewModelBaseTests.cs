using System.ComponentModel;
using Brinell.Scraper.ViewModels;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string? _name;
        private int _count;

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    [Fact]
    public void SetProperty_RaisesPropertyChanged_WhenValueChanges()
    {
        var vm = new TestViewModel();
        string? raised = null;
        vm.PropertyChanged += (_, e) => raised = e.PropertyName;

        vm.Name = "Alice";

        Assert.Equal("Name", raised);
    }

    [Fact]
    public void SetProperty_DoesNotRaise_WhenValueUnchanged()
    {
        var vm = new TestViewModel { Name = "Alice" };
        bool raised = false;
        vm.PropertyChanged += (_, _) => raised = true;

        vm.Name = "Alice";

        Assert.False(raised);
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenChanged()
    {
        var vm = new TestViewModel();
        bool changed = false;
        vm.PropertyChanged += (_, _) => changed = true;

        vm.Name = "Bob";

        Assert.True(changed);
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenUnchanged()
    {
        var vm = new TestViewModel { Count = 5 };
        bool raised = false;
        vm.PropertyChanged += (_, _) => raised = true;

        vm.Count = 5;

        Assert.False(raised);
    }

    [Fact]
    public void SetProperty_UpdatesBackingField()
    {
        var vm = new TestViewModel();

        vm.Name = "Charlie";

        Assert.Equal("Charlie", vm.Name);
    }

    [Fact]
    public void SetProperty_HandlesNullToValue()
    {
        var vm = new TestViewModel { Name = null };
        string? raised = null;
        vm.PropertyChanged += (_, e) => raised = e.PropertyName;

        vm.Name = "Delta";

        Assert.Equal("Delta", vm.Name);
        Assert.Equal("Name", raised);
    }

    [Fact]
    public void SetProperty_HandlesValueToNull()
    {
        var vm = new TestViewModel { Name = "Echo" };
        string? raised = null;
        vm.PropertyChanged += (_, e) => raised = e.PropertyName;

        vm.Name = null;

        Assert.Null(vm.Name);
        Assert.Equal("Name", raised);
    }

    [Fact]
    public void SetProperty_HandlesReferenceTypes()
    {
        var vm = new TestViewModel();
        var events = new List<string?>();
        vm.PropertyChanged += (_, e) => events.Add(e.PropertyName);

        vm.Name = "First";
        vm.Name = "Second";
        vm.Name = "Second";

        Assert.Equal(2, events.Count);
        Assert.Equal("Second", vm.Name);
    }

    [Fact]
    public void SetProperty_HandlesValueTypes()
    {
        var vm = new TestViewModel();
        var events = new List<string?>();
        vm.PropertyChanged += (_, e) => events.Add(e.PropertyName);

        vm.Count = 1;
        vm.Count = 1;
        vm.Count = 2;
        vm.Count = 0;

        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal("Count", e));
        Assert.Equal(0, vm.Count);
    }
}
