// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: PickerRevealAnimator.cs
// Description: Entrance / exit animations of the picker window ("eye-candy" reveal).
//              Everything animates the window's CONTENT (MainContainer / InnerContainer),
//              never the Window itself — WPF forbids RenderTransform on a Window.
//              All transforms/effects are created here in code so none of them is a
//              frozen XAML Freezable (see CLAUDE.md gotcha #7).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BrowserPickerPro.UI.Helpers;

public sealed class PickerRevealAnimator
{
    // ── Timing / shape constants (one place to tune the feel) ────────────────
    private static readonly TimeSpan FadeInFast   = TimeSpan.FromMilliseconds(90);
    private static readonly TimeSpan CircleDuration = TimeSpan.FromMilliseconds(620);
    private static readonly TimeSpan RingFadeDelay  = TimeSpan.FromMilliseconds(260);
    private static readonly TimeSpan RingFadeDuration = TimeSpan.FromMilliseconds(380);
    private static readonly TimeSpan ZoomDuration   = TimeSpan.FromMilliseconds(540);
    private static readonly TimeSpan BlurDuration   = TimeSpan.FromMilliseconds(480);
    private static readonly TimeSpan SpringDuration = TimeSpan.FromMilliseconds(780);
    private static readonly TimeSpan SpringScaleDuration = TimeSpan.FromMilliseconds(520);
    private static readonly TimeSpan PowerLineTime  = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan PowerOpenTime  = TimeSpan.FromMilliseconds(470);
    private static readonly TimeSpan PowerFlashHold = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan PowerFlashEnd  = TimeSpan.FromMilliseconds(680);
    private static readonly TimeSpan ShadowDuration = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CloseDuration  = TimeSpan.FromMilliseconds(150);

    private const double ZoomFromScale   = 1.16;   // starts slightly larger than final
    private const double ZoomFromBlur    = 36;     // px of blur at the start of ZoomBlur
    private const double SpringFromY     = 140;    // px below the final position
    private const double SpringFromScale = 0.86;
    private const double PowerLineHeight = 0.008;  // the thin bright "CRT line"
    private const double CloseToScale    = 0.94;
    private const double ShadowOpacity   = 0.75;   // matches the resting modal shadow

    /// <summary>The animations Random picks from (None is never picked).</summary>
    private static readonly PickerOpenAnimation[] RandomPool =
    {
        PickerOpenAnimation.CircleReveal, PickerOpenAnimation.ZoomBlur,
        PickerOpenAnimation.SpringRise, PickerOpenAnimation.PowerOn
    };

    private readonly FrameworkElement _outer;   // MainContainer: scale / translate / opacity
    private readonly FrameworkElement _inner;   // InnerContainer: clip / blur
    private readonly UIElement _flash;          // white overlay used by PowerOn
    private readonly Ellipse _ring;             // glowing ring used by CircleReveal
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly TranslateTransform _translate = new(0, 0);

    public PickerRevealAnimator(FrameworkElement outer, FrameworkElement inner, UIElement flash, Ellipse ring)
    {
        _outer = outer;
        _inner = inner;
        _flash = flash;
        _ring = ring;

        // Scale around the centre, then translate — one mutable group created in code.
        var group = new TransformGroup();
        group.Children.Add(_scale);
        group.Children.Add(_translate);
        _outer.RenderTransformOrigin = new Point(0.5, 0.5);
        _outer.RenderTransform = group;
    }

    /// <summary>
    /// Final animation to play: None when the user disabled animations or Windows asks
    /// for reduced motion ("Animation effects" off); Random resolved to a concrete one.
    /// </summary>
    public static PickerOpenAnimation Resolve(PickerOpenAnimation requested, bool animationsEnabled)
    {
        if (!animationsEnabled || !SystemParameters.ClientAreaAnimation) return PickerOpenAnimation.None;
        return requested == PickerOpenAnimation.Random
            ? RandomPool[System.Random.Shared.Next(RandomPool.Length)]
            : requested;
    }

    /// <summary>Stops every running animation and puts the content back at rest (visible).</summary>
    public void Reset()
    {
        _outer.BeginAnimation(UIElement.OpacityProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _translate.BeginAnimation(TranslateTransform.YProperty, null);
        _flash.BeginAnimation(UIElement.OpacityProperty, null);
        _ring.BeginAnimation(UIElement.OpacityProperty, null);
        _ring.BeginAnimation(FrameworkElement.WidthProperty, null);
        _ring.BeginAnimation(FrameworkElement.HeightProperty, null);
        _ring.BeginAnimation(Canvas.LeftProperty, null);
        _ring.BeginAnimation(Canvas.TopProperty, null);

        _outer.Opacity = 1;
        _scale.ScaleX = _scale.ScaleY = 1;
        _translate.Y = 0;
        _flash.Opacity = 0;
        _ring.Opacity = 0;
        _inner.Clip = null;
        _inner.Effect = null;
    }

    /// <summary>Hides the content before the window is shown, so no stale frame flashes.</summary>
    public void PrepareHidden()
    {
        Reset();
        _outer.Opacity = 0;
    }

    /// <summary>Plays the entrance. <paramref name="origin"/> is the cursor in InnerContainer coordinates.</summary>
    public void PlayOpen(PickerOpenAnimation kind, Point origin, DropShadowEffect? shadow)
    {
        Reset();
        if (shadow != null) Animate(shadow, DropShadowEffect.OpacityProperty, 0, ShadowOpacity, ShadowDuration, new CubicEase { EasingMode = EasingMode.EaseOut });

        switch (kind)
        {
            case PickerOpenAnimation.CircleReveal: CircleReveal(origin); break;
            case PickerOpenAnimation.ZoomBlur:     ZoomBlur(); break;
            case PickerOpenAnimation.SpringRise:   SpringRise(); break;
            case PickerOpenAnimation.PowerOn:      PowerOn(); break;
            default: break; // None: Reset() already left everything visible at rest
        }
    }

    /// <summary>Quick shrink + fade, then <paramref name="completed"/> (which hides the window).</summary>
    public void PlayClose(bool animate, Action completed)
    {
        if (!animate) { completed(); return; }

        _inner.Clip = null;
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        Animate(_scale, ScaleTransform.ScaleXProperty, _scale.ScaleX, CloseToScale, CloseDuration, ease);
        Animate(_scale, ScaleTransform.ScaleYProperty, _scale.ScaleY, CloseToScale, CloseDuration, ease);
        var fade = new DoubleAnimation(_outer.Opacity, 0, CloseDuration) { EasingFunction = ease };
        fade.Completed += (_, _) => completed();
        _outer.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    // ── Individual effects ───────────────────────────────────────────────────

    // A circular window onto the picker grows from the cursor, outlined by a glowing ring.
    private void CircleReveal(Point origin)
    {
        var w = Math.Max(_inner.ActualWidth, 1);
        var h = Math.Max(_inner.ActualHeight, 1);
        // Clamp the origin into the window so the circle starts at the edge nearest the cursor.
        origin = new Point(Math.Clamp(origin.X, 0, w), Math.Clamp(origin.Y, 0, h));
        // Radius that reaches the farthest corner → fully revealed at the end.
        var radius = new[] { new Point(0, 0), new Point(w, 0), new Point(0, h), new Point(w, h) }
            .Max(c => (c - origin).Length);

        var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 };
        var clip = new EllipseGeometry(origin, 0, 0);
        _inner.Clip = clip;

        var grow = new DoubleAnimation(0, radius, CircleDuration) { EasingFunction = ease };
        // Drop the clip once done: a clipped layered window costs more to render.
        grow.Completed += (_, _) => { if (ReferenceEquals(_inner.Clip, clip)) _inner.Clip = null; };
        clip.BeginAnimation(EllipseGeometry.RadiusXProperty, grow);
        clip.BeginAnimation(EllipseGeometry.RadiusYProperty, grow.Clone());

        // The ring tracks the clip edge: size 0→2r while its top-left moves origin→origin-r.
        Animate(_ring, FrameworkElement.WidthProperty, 0, radius * 2, CircleDuration, ease);
        Animate(_ring, FrameworkElement.HeightProperty, 0, radius * 2, CircleDuration, ease);
        Animate(_ring, Canvas.LeftProperty, origin.X, origin.X - radius, CircleDuration, ease);
        Animate(_ring, Canvas.TopProperty, origin.Y, origin.Y - radius, CircleDuration, ease);
        // Base value 1 is what shows until the delayed fade-out starts. Set it BEFORE
        // BeginAnimation (Reset() already released the property from older animations).
        _ring.Opacity = 1;
        _ring.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, RingFadeDuration)
        {
            BeginTime = RingFadeDelay,
            FillBehavior = FillBehavior.HoldEnd
        });

        Animate(_outer, UIElement.OpacityProperty, 0, 1, FadeInFast, null);
    }

    // Lens focusing: oversized + blurred → sharp at final size.
    private void ZoomBlur()
    {
        var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 };
        Animate(_scale, ScaleTransform.ScaleXProperty, ZoomFromScale, 1, ZoomDuration, ease);
        Animate(_scale, ScaleTransform.ScaleYProperty, ZoomFromScale, 1, ZoomDuration, ease);
        Animate(_outer, UIElement.OpacityProperty, 0, 1, TimeSpan.FromMilliseconds(220), null);

        // Blur on InnerContainer: MainContainer's single Effect slot holds the drop shadow.
        var blur = new BlurEffect { Radius = ZoomFromBlur, RenderingBias = RenderingBias.Performance };
        _inner.Effect = blur;
        var sharpen = new DoubleAnimation(ZoomFromBlur, 0, BlurDuration) { EasingFunction = ease };
        sharpen.Completed += (_, _) => { if (ReferenceEquals(_inner.Effect, blur)) _inner.Effect = null; };
        blur.BeginAnimation(BlurEffect.RadiusProperty, sharpen);
    }

    // Rises from below and overshoots once, like a card on a spring.
    private void SpringRise()
    {
        Animate(_translate, TranslateTransform.YProperty, SpringFromY, 0, SpringDuration,
            new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 5 });
        var back = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 };
        Animate(_scale, ScaleTransform.ScaleXProperty, SpringFromScale, 1, SpringScaleDuration, back);
        Animate(_scale, ScaleTransform.ScaleYProperty, SpringFromScale, 1, SpringScaleDuration, back);
        Animate(_outer, UIElement.OpacityProperty, 0, 1, TimeSpan.FromMilliseconds(180), null);
    }

    // Old CRT switching on: bright line opens sideways, then the picture opens vertically.
    private void PowerOn()
    {
        var x = new DoubleAnimationUsingKeyFrames();
        x.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        x.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(PowerLineTime),
            new CubicEase { EasingMode = EasingMode.EaseOut }));
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, x);

        var y = new DoubleAnimationUsingKeyFrames();
        y.KeyFrames.Add(new DiscreteDoubleKeyFrame(PowerLineHeight, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        y.KeyFrames.Add(new DiscreteDoubleKeyFrame(PowerLineHeight, KeyTime.FromTimeSpan(PowerLineTime)));
        y.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(PowerOpenTime),
            new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }));
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, y);

        var flash = new DoubleAnimationUsingKeyFrames();
        flash.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        flash.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(PowerFlashHold)));
        flash.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(PowerFlashEnd),
            new CubicEase { EasingMode = EasingMode.EaseOut }));
        _flash.BeginAnimation(UIElement.OpacityProperty, flash);

        Animate(_outer, UIElement.OpacityProperty, 0, 1, TimeSpan.FromMilliseconds(30), null);
    }

    // Small helper: From/To animation on any animatable target.
    private static void Animate(IAnimatable target, DependencyProperty property, double from, double to,
                                TimeSpan duration, IEasingFunction? ease)
        => target.BeginAnimation(property, new DoubleAnimation(from, to, duration) { EasingFunction = ease });
}
