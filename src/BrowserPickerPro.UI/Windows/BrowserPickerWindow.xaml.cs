// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

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
        AnimationHelper.FadeIn(this, 220);
        SearchBox.Focus();

        if (DataContext is BrowserPickerViewModel vm && vm.Settings is AppSettings settings)
            ApplyPickerSettings(settings);
    }

    private void ApplyPickerSettings(AppSettings settings)
    {
        // Dark tint opacity
        DarkTintOverlay.Opacity = settings.PickerDarkTintOpacity;

        // Background
        if (settings.PickerBackgroundMode == PickerBackgroundMode.CustomFile
            && !string.IsNullOrEmpty(settings.PickerBackgroundImagePath)
            && File.Exists(settings.PickerBackgroundImagePath))
        {
            var path = settings.PickerBackgroundImagePath;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".mp4" or ".webm" or ".avi" or ".gif")
            {
                BgMedia.Source = new Uri(path);
                BgMedia.Visibility = Visibility.Visible;
                BgGradient.Visibility = Visibility.Collapsed;
            }
            else
            {
                try
                {
                    BgImage.Source = new BitmapImage(new Uri(path));
                    BgImage.Visibility = Visibility.Visible;
                    BgGradient.Visibility = Visibility.Collapsed;
                }
                catch { /* fallback to gradient */ }
            }
        }

        // Fullscreen vs Modal
        if (settings.PickerWindowStyle == PickerWindowStyle.Fullscreen)
        {
            WindowState = WindowState.Maximized;
            MainContainer.Margin = new Thickness(0);
            MainContainer.MaxWidth = double.PositiveInfinity;
            MainContainer.MaxHeight = double.PositiveInfinity;
            MainContainer.CornerRadius = new CornerRadius(0);
            MainContainer.Effect = null;
            InnerContainer.CornerRadius = new CornerRadius(0);
        }

        // Card layout
        if (settings.PickerCardLayout == PickerCardLayout.SingleRow)
        {
            var factory = new FrameworkElementFactory(typeof(StackPanel));
            factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            BrowserItemsControl.ItemsPanel = new ItemsPanelTemplate(factory);
            BrowserScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            BrowserScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
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
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrowserPickerViewModel vm)
            vm.CancelCommand.Execute(null);
        else
            Close();
    }

    // ── Drag-to-reorder ──────────────────────────────────────────────────────

    private void BrowserCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => _dragStartPoint = e.GetPosition(null);

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
        if (DataContext is BrowserPickerViewModel vm) vm.MoveItem(_draggedItem, target);
    }

    // ── 3D card tilt ─────────────────────────────────────────────────────────

    // Returns (or creates fresh) a 3-child TransformGroup: Scale, Skew, Translate.
    // Never touches frozen transforms — always creates new ones in code.
    private static TransformGroup EnsureTiltTransform(Button btn)
    {
        if (btn.RenderTransform is TransformGroup tg && !tg.IsFrozen && tg.Children.Count >= 3)
            return tg;
        var newTg = new TransformGroup();
        newTg.Children.Add(new ScaleTransform(1, 1));
        newTg.Children.Add(new SkewTransform(0, 0));
        newTg.Children.Add(new TranslateTransform(0, 0));
        btn.RenderTransformOrigin = new Point(0.5, 0.5);
        btn.RenderTransform = newTg;
        return newTg;
    }

    // Returns (or creates) a mutable DropShadowEffect attached to the button.
    private static DropShadowEffect EnsureShadow(Button btn)
    {
        if (btn.Effect is DropShadowEffect dse && !dse.IsFrozen) return dse;
        var shadow = new DropShadowEffect
        {
            ShadowDepth = 4,
            Direction = 270,
            BlurRadius = 16,
            Opacity = 0.15,
            Color = Colors.Black
        };
        btn.Effect = shadow;
        return shadow;
    }

    // Walks the visual tree to find the specular shine overlay Border by Tag.
    private static Border? FindShineLayer(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Border b && b.Tag is string tag && tag == "ShineLayer")
                return b;
            var found = FindShineLayer(child);
            if (found != null) return found;
        }
        return null;
    }

    private void BrowserCard_Tilt(object sender, MouseEventArgs e)
    {
        if (_isDragging || sender is not Button btn) return;

        var tg = EnsureTiltTransform(btn);
        var shadow = EnsureShadow(btn);

        var pos = e.GetPosition(btn);
        var dx = btn.ActualWidth  > 0 ? (pos.X - btn.ActualWidth  / 2) / (btn.ActualWidth  / 2) : 0;
        var dy = btn.ActualHeight > 0 ? (pos.Y - btn.ActualHeight / 2) / (btn.ActualHeight / 2) : 0;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur  = TimeSpan.FromMilliseconds(100);

        // Scale — card pops toward viewer
        if (tg.Children[0] is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.08, dur) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.08, dur) { EasingFunction = ease });
        }

        // Skew — perspective-like tilt following the cursor (±14 degrees max)
        if (tg.Children[1] is SkewTransform skew)
        {
            skew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation(dy * -14, dur) { EasingFunction = ease });
            skew.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation(dx *  14, dur) { EasingFunction = ease });
        }

        // Translate — subtle lift off the surface
        if (tg.Children[2] is TranslateTransform translate)
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-5, dur) { EasingFunction = ease });

        // Shadow — grows to show elevation
        shadow.ShadowDepth = 18;
        shadow.BlurRadius   = 32;
        shadow.Opacity      = 0.50;
        shadow.Direction    = 270;

        // Specular shine — radial white gradient that follows the cursor
        var shine = FindShineLayer(btn);
        if (shine != null)
        {
            if (shine.Background is not RadialGradientBrush || shine.Background.IsFrozen)
            {
                var brush = new RadialGradientBrush();
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(65, 255, 255, 255), 0));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,  255, 255, 255), 1));
                brush.RadiusX = 0.70;
                brush.RadiusY = 0.70;
                shine.Background = brush;
            }

            if (shine.Background is RadialGradientBrush rgb && btn.ActualWidth > 0 && btn.ActualHeight > 0)
            {
                var cx = pos.X / btn.ActualWidth;
                var cy = pos.Y / btn.ActualHeight;
                rgb.Center        = new Point(cx, cy);
                rgb.GradientOrigin = new Point(cx, cy);
            }

            shine.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));
        }
    }

    private void BrowserCard_ResetTilt(object sender, MouseEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.RenderTransform is not TransformGroup tg || tg.IsFrozen || tg.Children.Count < 3) return;

        // ElasticEase spring-back — the card bounces slightly past zero before settling,
        // giving it the feeling of a physical card snapping back into place.
        var spring = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 5 };
        var dur    = TimeSpan.FromMilliseconds(480);

        if (tg.Children[0] is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, dur) { EasingFunction = spring });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, dur) { EasingFunction = spring });
        }
        if (tg.Children[1] is SkewTransform skew)
        {
            skew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation(0, dur) { EasingFunction = spring });
            skew.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation(0, dur) { EasingFunction = spring });
        }
        if (tg.Children[2] is TranslateTransform translate)
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, dur) { EasingFunction = spring });

        // Shadow shrinks back
        if (btn.Effect is DropShadowEffect dse && !dse.IsFrozen)
        {
            dse.ShadowDepth = 4;
            dse.BlurRadius   = 16;
            dse.Opacity      = 0.15;
            dse.Direction    = 270;
        }

        // Shine fades out
        var shine = FindShineLayer(btn);
        if (shine != null)
            shine.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
    }

    // ── Background media loop ─────────────────────────────────────────────────

    private void BgMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        BgMedia.Position = TimeSpan.Zero;
        BgMedia.Play();
    }
}
