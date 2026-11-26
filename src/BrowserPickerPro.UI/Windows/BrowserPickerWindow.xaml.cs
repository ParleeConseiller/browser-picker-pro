// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: BrowserPickerWindow.xaml.cs
// Description: Code-behind of the picker window: settings application, pre-warming,
//              reveal/conceal (resident tray mode), background video control,
//              drag-to-reorder and the 3D card hover effect.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 2.0.0
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BrowserPickerPro.UI.Windows;

public partial class BrowserPickerWindow : Window
{
    // Modal-mode geometry (matches the XAML defaults; restored when leaving Fullscreen).
    private static readonly Thickness ModalMargin = new(32);
    private const double ModalMaxWidth = 960;
    private const double ModalMaxHeight = 600;
    private const double ModalCornerRadius = 22;
    // Window position used while pre-warming: far off every monitor.
    private const int OffscreenPosition = -32000;
    // File extensions played by MediaElement instead of shown as a still image.
    private static readonly string[] MediaExtensions = { ".mp4", ".webm", ".avi", ".gif", ".wmv", ".mov", ".mkv" };

    private readonly ILogger<BrowserPickerWindow>? _logger;
    private readonly PickerRevealAnimator _animator;
    // Mutable shadow created in code so the reveal can fade it in (XAML one would be frozen).
    private readonly DropShadowEffect _modalShadow = new() { ShadowDepth = 0, BlurRadius = 70, Opacity = 0.75, Color = Colors.Black };

    private Point _dragStartPoint;
    private BrowserItemViewModel? _draggedItem;
    private bool _isDragging;

    private AppSettings? _settings;
    private string? _appliedBackgroundPath;   // what BgMedia/BgImage currently hold
    private bool _backgroundIsMedia;
    private bool _revealed;                    // logically visible (between Reveal and Conceal)
    private bool _allowClose;                  // true only when the app really exits

    /// <summary>
    /// Resident (tray) mode: closing, Escape and minimise hide the window instead of
    /// destroying it, so the next link reuses the warm window and video.
    /// </summary>
    public bool IsResident { get; set; }

    public BrowserPickerWindow(ILogger<BrowserPickerWindow>? logger = null)
    {
        InitializeComponent();
        _logger = logger;
        _animator = new PickerRevealAnimator(MainContainer, InnerContainer, RevealFlash, RevealRing);
        KeyDown += OnKeyDown;
        Closing += OnClosing;
        // Never in Alt+Tab / taskbar: the resident picker stays shown (parked) between links.
        SourceInitialized += (_, _) => NativeMethods.MakeToolWindow(new WindowInteropHelper(this).Handle);
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the appearance settings. Idempotent and cheap when nothing changed — the
    /// background video is only reloaded when its path changes, so it stays warm.
    /// </summary>
    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;

        DarkTintOverlay.Opacity = settings.PickerDarkTintOpacity;
        ApplyBackground(settings);

        // Window style and card layout are applied only when they CHANGE: this runs on every
        // link, and replacing ItemsPanel makes WPF regenerate every card container (slow).
        if (_appliedStyle != settings.PickerWindowStyle)
        {
            _appliedStyle = settings.PickerWindowStyle;
            ApplyWindowStyle(settings.PickerWindowStyle);
        }
        if (_appliedLayout != settings.PickerCardLayout)
        {
            _appliedLayout = settings.PickerCardLayout;
            ApplyCardLayout(settings.PickerCardLayout);
        }
    }

    private PickerWindowStyle? _appliedStyle;
    private PickerCardLayout? _appliedLayout;

    // Sets the window style BOTH ways — the resident window can switch at runtime.
    private void ApplyWindowStyle(PickerWindowStyle style)
    {
        if (style == PickerWindowStyle.Fullscreen)
        {
            MainContainer.Margin = new Thickness(0);
            MainContainer.MaxWidth = double.PositiveInfinity;
            MainContainer.MaxHeight = double.PositiveInfinity;
            MainContainer.CornerRadius = new CornerRadius(0);
            MainContainer.Effect = null;
            InnerContainer.CornerRadius = new CornerRadius(0);
        }
        else
        {
            WindowState = WindowState.Normal;
            MainContainer.Margin = ModalMargin;
            MainContainer.MaxWidth = ModalMaxWidth;
            MainContainer.MaxHeight = ModalMaxHeight;
            MainContainer.CornerRadius = new CornerRadius(ModalCornerRadius);
            MainContainer.Effect = _modalShadow;
            InnerContainer.CornerRadius = new CornerRadius(ModalCornerRadius);
        }
    }

    // Card layout, also both ways.
    private void ApplyCardLayout(PickerCardLayout layout)
    {
        var horizontal = layout == PickerCardLayout.SingleRow;
        var factory = new FrameworkElementFactory(horizontal ? typeof(StackPanel) : typeof(WrapPanel));
        if (horizontal) factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        else factory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        BrowserItemsControl.ItemsPanel = new ItemsPanelTemplate(factory);
        BrowserScrollViewer.HorizontalScrollBarVisibility = horizontal ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        BrowserScrollViewer.VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    private void ApplyBackground(AppSettings settings)
    {
        var path = settings.PickerBackgroundMode == PickerBackgroundMode.CustomFile
                   && !string.IsNullOrEmpty(settings.PickerBackgroundImagePath)
                   && File.Exists(settings.PickerBackgroundImagePath)
            ? settings.PickerBackgroundImagePath
            : null;

        if (string.Equals(path, _appliedBackgroundPath, StringComparison.OrdinalIgnoreCase)) return;
        _appliedBackgroundPath = path;

        // Tear down the previous background.
        BgMedia.Stop();
        BgMedia.Source = null;
        BgMedia.Visibility = Visibility.Collapsed;
        BgImage.Source = null;
        BgImage.Visibility = Visibility.Collapsed;
        BgGradient.Visibility = Visibility.Visible;
        _backgroundIsMedia = false;

        if (path is null) return;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (MediaExtensions.Contains(ext))
        {
            _backgroundIsMedia = true;
            BgMedia.Source = new Uri(path);
            BgMedia.Visibility = Visibility.Visible;
            BgGradient.Visibility = Visibility.Collapsed;
            // Manual mode: Play() opens the file; MediaOpened pauses it again if we're hidden.
            BgMedia.Play();
        }
        else
        {
            try
            {
                // OnLoad + Freeze: decoded once, file not kept locked, safe to share.
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                bmp.Freeze();
                BgImage.Source = bmp;
                BgImage.Visibility = Visibility.Visible;
                BgGradient.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Background image could not be loaded: {Path}", path);
            }
        }
    }

    // ── Pre-warm / reveal / conceal ──────────────────────────────────────────
    //
    // Resident mode never Hide()s the window between links: it stays shown but PARKED far
    // off-screen with its content at opacity 0. Revealing is then a window move instead of
    // Show(), which on this transparent (layered) window cost ~55 ms of the ~100 ms budget.
    // It is a tool window (no Alt+Tab / taskbar entry), the video is paused while parked,
    // and on cancel the previous foreground window gets the focus back.

    /// <summary>
    /// Shows the window once, off-screen and transparent, so WPF builds the HWND, the visual
    /// tree and the video pipeline; it then stays parked there until the first link.
    /// </summary>
    public async Task PrewarmAsync()
    {
        _animator.PrepareHidden();
        ShowActivated = false;
        Left = OffscreenPosition;
        Top = OffscreenPosition;
        Show();

        // Let one frame render so templates/bindings are realised.
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);

        // Do NOT wait for the video to open here: on a cold start the first link is waiting
        // on this method (that wait made the cold path 1 s slower than v1). The decoder keeps
        // opening in the background; BgMedia_MediaOpened pauses it if the picker is parked,
        // and a Reveal() that comes first has already called Play().
        if (!_revealed && _backgroundIsMedia) BgMedia.Pause();
        ShowActivated = true;
    }

    /// <summary>True between Reveal() and Conceal() — i.e. the user can see the picker.</summary>
    public bool IsRevealed => _revealed;

    /// <summary>
    /// Brings the picker onto the monitor under the cursor and plays the opening animation.
    /// <paramref name="launcherTimestamp"/> (QPC ticks, 0 = unknown) is used to log the true
    /// click-to-first-frame latency.
    /// </summary>
    public void Reveal(long launcherTimestamp = 0)
    {
        var settings = _settings ?? new AppSettings();
        var hwnd = new WindowInteropHelper(this).EnsureHandle();

        // Remember who had the focus, to give it back if the user cancels.
        if (!_revealed)
        {
            var fg = NativeMethods.GetForegroundWindow();
            _previousForeground = fg != hwnd ? fg : IntPtr.Zero;
        }
        _revealed = true;

        _animator.PrepareHidden();
        var cursor = NativeMethods.CursorPosition();
        PositionOnMonitor(cursor, settings.PickerWindowStyle == PickerWindowStyle.Fullscreen);

        Topmost = true;                // a picker must not open behind the app the link came from
        if (!IsVisible) Show();        // legacy mode / first use; resident mode is already shown
        Activate();
        NativeMethods.SetForegroundWindow(hwnd);

        if (_backgroundIsMedia) BgMedia.Play();

        var kind = PickerRevealAnimator.Resolve(settings.PickerOpenAnimation, settings.EnableAnimations);
        // Cursor in InnerContainer coordinates = where the CircleReveal starts.
        var origin = InnerContainer.PointFromScreen(new Point(cursor.X, cursor.Y));
        _animator.PlayOpen(kind, origin, MainContainer.Effect as DropShadowEffect);

        // Keyboard: type to search immediately.
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { SearchBox.Focus(); Keyboard.Focus(SearchBox); });

        if (launcherTimestamp > 0) LogFirstFrame(launcherTimestamp, kind);
    }

    /// <summary>
    /// Plays the exit animation, then parks the window off-screen and pauses the video.
    /// <paramref name="restoreFocus"/>: give the focus back to the app the link came from
    /// (cancel); false after a pick, because the launched browser takes the focus.
    /// </summary>
    public void Conceal(bool restoreFocus)
    {
        if (!_revealed) return;
        _revealed = false;

        var animate = _settings?.EnableAnimations != false && SystemParameters.ClientAreaAnimation;
        _animator.PlayClose(animate, () =>
        {
            if (_revealed) return; // a new link arrived during the exit animation
            Topmost = false;
            if (_backgroundIsMedia) BgMedia.Pause();
            WindowState = WindowState.Normal;

            if (IsResident) Park();
            else Hide();

            if (restoreFocus && _previousForeground != IntPtr.Zero && NativeMethods.IsWindow(_previousForeground))
                NativeMethods.SetForegroundWindow(_previousForeground);
            _previousForeground = IntPtr.Zero;
        });
    }

    // Moves the (still shown, fully transparent) window far off every monitor.
    private void Park()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero) NativeMethods.MoveWindow(hwnd, OffscreenPosition, OffscreenPosition);
    }

    private IntPtr _previousForeground;

    /// <summary>Lets the window really close (application exit).</summary>
    public void CloseForExit()
    {
        _allowClose = true;
        Close();
    }

    // Centres the window on the work area of the monitor under the cursor (physical pixels,
    // so mixed-DPI setups land correctly); Fullscreen maximises on that monitor.
    private void PositionOnMonitor(NativeMethods.POINT cursor, bool fullscreen)
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        var (work, _, scale) = NativeMethods.MonitorUnder(cursor);

        WindowState = WindowState.Normal;
        var widthPx = (int)Math.Round(Width * scale);
        var heightPx = (int)Math.Round(Height * scale);
        var x = work.Left + Math.Max(0, (work.Right - work.Left - widthPx) / 2);
        var y = work.Top + Math.Max(0, (work.Bottom - work.Top - heightPx) / 2);
        NativeMethods.MoveWindow(hwnd, x, y);

        if (fullscreen) WindowState = WindowState.Maximized;
    }

    // Logs "click → first frame on screen" once, on the next composition frame.
    private void LogFirstFrame(long launcherTimestamp, PickerOpenAnimation kind)
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            CompositionTarget.Rendering -= handler;
            var ms = Stopwatch.GetElapsedTime(launcherTimestamp).TotalMilliseconds;
            _logger?.LogInformation("Picker first frame {Ms:F1} ms after link click (animation {Animation})", ms, kind);
        };
        CompositionTarget.Rendering += handler;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!IsResident || _allowClose) return;
        // Alt+F4 / WM_CLOSE on the resident picker = cancel the pick and hide.
        e.Cancel = true;
        RequestCancel();
    }

    private void RequestCancel()
    {
        if (DataContext is BrowserPickerViewModel vm) vm.CancelCommand.Execute(null);
        else if (IsResident) Conceal(restoreFocus: true);
        else Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) RequestCancel();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // Resident: "minimise" means back to the tray.
        if (IsResident) RequestCancel();
        else WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => RequestCancel();

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

    // Video decoder is ready: pause straight away while the picker is hidden (pre-warm),
    // so it costs no CPU/GPU until the next Reveal() calls Play().
    private void BgMedia_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (!_revealed) BgMedia.Pause();
    }

    // Seamless loop: rewind and keep playing.
    private void BgMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        BgMedia.Position = TimeSpan.Zero;
        BgMedia.Play();
    }
}
