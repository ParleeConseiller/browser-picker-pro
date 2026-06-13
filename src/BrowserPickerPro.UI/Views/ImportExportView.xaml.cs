// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using System.Windows.Controls;

namespace BrowserPickerPro.UI.Views;

public partial class ImportExportView : Page
{
    public ImportExportView(ImportExportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => AnimationHelper.SlideIn(this);
    }
}
