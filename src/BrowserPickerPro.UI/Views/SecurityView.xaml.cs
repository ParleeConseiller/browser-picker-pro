// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using System.Windows.Controls;

namespace BrowserPickerPro.UI.Views;

public partial class SecurityView : Page
{
    public SecurityView(SecurityViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            await viewModel.InitializeAsync();
            AnimationHelper.SlideIn(this);
        };
    }
}
