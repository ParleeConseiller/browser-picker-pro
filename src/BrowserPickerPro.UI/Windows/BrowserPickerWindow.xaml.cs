// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrowserPickerPro.UI.Windows;

public partial class BrowserPickerWindow : Window
{
    private Point _dragStartPoint;
    private BrowserItemViewModel? _draggedItem;
    private bool _isDragging;

    public BrowserPickerWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var isDark = Helpers.ThemeHelper.IsSystemDarkMode();
        MicaHelper.TryApplyAcrylic(this, isDark);
        AnimationHelper.FadeIn(this, 200);
        SearchBox.Focus();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is BrowserPickerViewModel vm)
                vm.CancelCommand.Execute(null);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrowserPickerViewModel vm)
            vm.CancelCommand.Execute(null);
        else
            Close();
    }

    // --- Drag-to-reorder ---

    private void BrowserCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void BrowserCard_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

        var pos = e.GetPosition(null);
        var diff = pos - _dragStartPoint;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        if (sender is not Button btn || btn.Tag is not BrowserItemViewModel vm) return;

        _draggedItem = vm;
        _isDragging = true;
        DragDrop.DoDragDrop(btn, vm, DragDropEffects.Move);
        _isDragging = false;
        _draggedItem = null;
    }

    private void BrowserCard_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = _draggedItem != null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void BrowserCard_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Button btn && btn.Tag is BrowserItemViewModel target && target != _draggedItem)
            btn.Opacity = 0.45;
    }

    private void BrowserCard_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Button btn) btn.Opacity = 1.0;
    }

    private void BrowserCard_Drop(object sender, DragEventArgs e)
    {
        if (sender is Button btn) btn.Opacity = 1.0;
        if (sender is not Button dropBtn || dropBtn.Tag is not BrowserItemViewModel target) return;
        if (_draggedItem == null || ReferenceEquals(_draggedItem, target)) return;

        if (DataContext is BrowserPickerViewModel vm)
            vm.MoveItem(_draggedItem, target);
    }
}
