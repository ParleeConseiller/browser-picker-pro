// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using System.Windows.Controls;

namespace BrowserPickerPro.UI.Services;

public class NavigationService
{
    private Frame? _frame;
    private readonly Dictionary<NavigationSection, Func<Page>> _pageFactories = new();

    public NavigationSection CurrentSection { get; private set; }
    public event EventHandler<NavigationSection>? Navigated;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void RegisterPage(NavigationSection section, Func<Page> factory)
    {
        _pageFactories[section] = factory;
    }

    public void NavigateTo(NavigationSection section)
    {
        if (_frame == null || !_pageFactories.TryGetValue(section, out var factory)) return;
        CurrentSection = section;
        _frame.Navigate(factory());
        Navigated?.Invoke(this, section);
    }
}
