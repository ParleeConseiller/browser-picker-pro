// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using System.Windows;
using System.Windows.Media.Animation;

namespace BrowserPickerPro.UI.Helpers;

public static class AnimationHelper
{
    public static void FadeIn(UIElement element, double durationMs = 200, double targetOpacity = 1.0)
    {
        var animation = new DoubleAnimation(0, targetOpacity, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    public static void FadeOut(UIElement element, double durationMs = 150, Action? onComplete = null)
    {
        var animation = new DoubleAnimation(element.Opacity, 0, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        if (onComplete != null)
            animation.Completed += (_, _) => onComplete();
        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    public static void SlideIn(FrameworkElement element, double fromX = 20, double durationMs = 250)
    {
        var transform = new System.Windows.Media.TranslateTransform(fromX, 0);
        element.RenderTransform = transform;
        element.Opacity = 0;

        var slideAnimation = new DoubleAnimation(fromX, 0, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideAnimation);
        element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
    }

    public static void ScalePop(FrameworkElement element, double durationMs = 200)
    {
        var transform = new System.Windows.Media.ScaleTransform(0.95, 0.95)
        {
            CenterX = element.ActualWidth / 2,
            CenterY = element.ActualHeight / 2
        };
        element.RenderTransform = transform;
        element.Opacity = 0;

        var scaleX = new DoubleAnimation(0.95, 1.0, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
        };
        var scaleY = new DoubleAnimation(0.95, 1.0, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
        };
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs * 0.6))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleX);
        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleY);
        element.BeginAnimation(UIElement.OpacityProperty, fade);
    }
}
