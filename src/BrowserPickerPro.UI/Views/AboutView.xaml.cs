// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using System.Windows.Controls;

namespace BrowserPickerPro.UI.Views;

public partial class AboutView : Page
{
    public AboutView(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => AnimationHelper.SlideIn(this);
    }
}
