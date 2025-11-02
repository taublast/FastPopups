#if MACCATALYST || IOS

using AppoMobi.Maui.FastPopups;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using UIKit;

namespace FastPopups.Platforms.iOS;

/// <summary>
/// Handles iOS/macOS-specific popup animations using UIView animation APIs.
/// </summary>
public class PopupAnimator
{
    private bool _isAnimating;
    private bool _hasShownOnce;

    /// <summary>
    /// Prepares the initial state for show animation.
    /// Call this BEFORE the view is shown, to set initial invisible state.
    /// </summary>
    public void PrepareShowAnimation(
        UIView contentView,
        UIView overlayView,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing)
    {
        if (animationType == PopupAnimationType.None)
        {
            // No animation - just make visible
            contentView.Alpha = 1f;
            overlayView.Alpha = 1f;
            return;
        }

        // Set initial state BEFORE view is shown
        SetInitialAnimationState(contentView, overlayView, animationType);
    }

    /// <summary>
    /// Animates the popup content and overlay when showing.
    /// Call this AFTER the view is shown and has proper size.
    /// </summary>
    public async Task AnimateShowAsync(
        UIView contentView,
        UIView overlayView,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing,
        CancellationToken cancellationToken = default)
    {
        if (_isAnimating || _hasShownOnce)
        {
            return;
        }

        _hasShownOnce = true;
        _isAnimating = true;

        try
        {
            if (animationType == PopupAnimationType.None)
            {
                // Already set in PrepareShowAnimation
                return;
            }

            // Adjust duration for WhirlIn3
            var durationSeconds = animationType == PopupAnimationType.WhirlIn3
                ? Math.Max(duration, 400) / 1000.0
                : duration / 1000.0;

            // FORCE Elastic easing for Elastic animation types (regardless of user's easing setting)
            if (IsElasticAnimation(animationType))
            {
                easing = PopupAnimationEasing.Elastic;
            }

            // Set anchor point (pivot) to CENTER for scale/rotation animations
            if (NeedsCenterAnchor(animationType))
            {
                SetAnchorPoint(contentView, new CGPoint(0.5, 0.5));
            }

            // Special handling for Flip animations: Use CABasicAnimation for smooth 3D rotation
            if (animationType == PopupAnimationType.FlipHorizontal || animationType == PopupAnimationType.FlipVertical)
            {
                // Set stronger perspective for better 3D effect
                var transform = CATransform3D.Identity;
                transform.M34 = -1.0f / 200.0f; // Stronger perspective (was 500)
                contentView.Layer.Transform = transform;

                // Create rotation animation
                var keyPath = animationType == PopupAnimationType.FlipHorizontal ? "transform.rotation.y" : "transform.rotation.x";
                var flipAnimation = CABasicAnimation.FromKeyPath(keyPath);
                flipAnimation.From = new NSNumber(-Math.PI / 2); // -90°
                flipAnimation.To = new NSNumber(0); // 0°
                flipAnimation.Duration = durationSeconds;
                flipAnimation.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
                flipAnimation.FillMode = CAFillMode.Forwards;
                flipAnimation.RemovedOnCompletion = false;
                contentView.Layer.AddAnimation(flipAnimation, "flipShowRotation");
            }

            // Create property animator with timing curve
            var timingParameters = GetTimingParameters(easing);
            var animator = new UIViewPropertyAnimator(durationSeconds, timingParameters);

            // Add animations
            animator.AddAnimations(() =>
            {
                // Animate overlay (always linear fade)
                overlayView.Alpha = 1f;
                contentView.Alpha = 1;

                // Animate content based on type
                AnimateShowContent(contentView, overlayView, animationType);
            });

            // Create task completion source
            var tcs = new TaskCompletionSource<bool>();
            animator.AddCompletion((position) =>
            {
                Cleanup(contentView, overlayView);
                tcs.TrySetResult(position == UIViewAnimatingPosition.End);
            });

            // Start animation
            animator.StartAnimation();

            await tcs.Task;
        }
        finally
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// Animates the popup content and overlay when hiding.
    /// </summary>
    public async Task AnimateHideAsync(
        UIView contentView,
        UIView overlayView,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing,
        CancellationToken cancellationToken = default)
    {
        if (_isAnimating)
        {
            return;
        }

        _isAnimating = true;

        try
        {
            // DEBUG: Check what transform we ACTUALLY have before starting hide
            System.Diagnostics.Debug.WriteLine($"=== AnimateHideAsync START ===");
            System.Diagnostics.Debug.WriteLine($"Animation Type: {animationType}");
            System.Diagnostics.Debug.WriteLine($"contentView.Transform BEFORE anything: {contentView.Transform}");
            System.Diagnostics.Debug.WriteLine($"contentView.Layer.Transform: {contentView.Layer.Transform}");
            System.Diagnostics.Debug.WriteLine($"contentView.Frame: {contentView.Frame}");
            System.Diagnostics.Debug.WriteLine($"contentView.Bounds: {contentView.Bounds}");
            System.Diagnostics.Debug.WriteLine($"contentView.Center: {contentView.Center}");

            if (animationType == PopupAnimationType.None)
            {
                contentView.Alpha = 0f;
                overlayView.Alpha = 0f;
                return;
            }

            // Adjust duration for WhirlIn3
            var durationSeconds = animationType == PopupAnimationType.WhirlIn3
                ? Math.Max(duration, 400) / 1000.0
                : duration / 1000.0;

            // FORCE Elastic easing for Elastic animation types (regardless of user's easing setting)
            if (IsElasticAnimation(animationType))
            {
                easing = PopupAnimationEasing.Elastic;
            }

            // Set anchor point (pivot) to CENTER for scale/rotation animations
            if (NeedsCenterAnchor(animationType))
            {
                SetAnchorPoint(contentView, new CGPoint(0.5, 0.5));
            }

            // Special handling for WhirlIn/WhirlIn3: Add rotation animation separately
            if (animationType == PopupAnimationType.WhirlIn || animationType == PopupAnimationType.WhirlIn3)
            {
                // HIDE rotates in OPPOSITE direction from SHOW (CCW instead of CW)
                var rotationRadians = animationType == PopupAnimationType.WhirlIn3 ? -Math.PI * 6 : -Math.PI;
                var whirlDuration = animationType == PopupAnimationType.WhirlIn3 ? 0.4 : durationSeconds;

                var hideRotation = CABasicAnimation.FromKeyPath("transform.rotation.z");
                hideRotation.From = new NSNumber(0);
                hideRotation.To = new NSNumber(rotationRadians);
                hideRotation.Duration = whirlDuration;
                hideRotation.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
                hideRotation.FillMode = CAFillMode.Forwards;
                hideRotation.RemovedOnCompletion = false;
                contentView.Layer.AddAnimation(hideRotation, "whirlHideRotation");
            }

            // Special handling for Flip animations: Use CABasicAnimation for smooth 3D rotation
            if (animationType == PopupAnimationType.FlipHorizontal || animationType == PopupAnimationType.FlipVertical)
            {
                // Set stronger perspective for better 3D effect
                var transform = CATransform3D.Identity;
                transform.M34 = -1.0f / 200.0f; // Stronger perspective
                contentView.Layer.Transform = transform;

                // Create rotation animation - HIDE rotates opposite direction (to +90°)
                var keyPath = animationType == PopupAnimationType.FlipHorizontal ? "transform.rotation.y" : "transform.rotation.x";
                var flipAnimation = CABasicAnimation.FromKeyPath(keyPath);
                flipAnimation.From = new NSNumber(0); // 0°
                flipAnimation.To = new NSNumber(Math.PI / 2); // +90°
                flipAnimation.Duration = durationSeconds;
                flipAnimation.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
                flipAnimation.FillMode = CAFillMode.Forwards;
                flipAnimation.RemovedOnCompletion = false;
                contentView.Layer.AddAnimation(flipAnimation, "flipHideRotation");
            }

            // Create property animator with timing curve
            var timingParameters = GetTimingParameters(easing);
            var animator = new UIViewPropertyAnimator(durationSeconds, timingParameters);

            // Add animations
            animator.AddAnimations(() =>
            {
                // Animate overlay (always linear fade)
                overlayView.Alpha = 0f;

                // Animate content based on type
                AnimateHideContent(contentView, overlayView, animationType);
            });

            // Create task completion source
            var tcs = new TaskCompletionSource<bool>();
            animator.AddCompletion((position) =>
            {
                tcs.TrySetResult(position == UIViewAnimatingPosition.End);
            });

            // Start animation
            animator.StartAnimation();

            await tcs.Task;
        }
        finally
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// Cleans up animation resources.
    /// </summary>
    public void Cleanup(UIView contentView, UIView overlayView)
    {
        // Reset view properties to true Identity
        contentView.Transform = CGAffineTransform.MakeIdentity();
        contentView.Layer.Transform = CATransform3D.Identity;
        contentView.Alpha = 1f;
        overlayView.Alpha = 1f;

        // Reset anchor point to center
        SetAnchorPoint(contentView, new CGPoint(0.5, 0.5));
    }

    /// <summary>
    /// Sets initial animation state (public for external control).
    /// </summary>
    public void SetInitialAnimationState(UIView view, UIView viewContainer, PopupAnimationType animationType)
    {
        view.Layer.ZPosition = 9999; //solves flip etc covered by dimmer layer

        // Always start overlay as invisible
        viewContainer.Alpha = 0f;

        var containerHeight = viewContainer.Bounds.Height;
        var containerWidth = viewContainer.Bounds.Width;

        // Set content initial state based on animation type
        switch (animationType)
        {
        case PopupAnimationType.Default:
        case PopupAnimationType.Fade:
        view.Alpha = 0f;
        break;

        case PopupAnimationType.FromBottomElastic:
        case PopupAnimationType.FromBottom:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(0, containerHeight - view.Frame.Y);
        break;

        case PopupAnimationType.FromTopElastic:
        case PopupAnimationType.FromTop:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(0, -(view.Frame.Y + view.Frame.Height));
        break;

        case PopupAnimationType.FromRightElastic:
        case PopupAnimationType.FromRight:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(containerWidth - view.Frame.X, 0);
        break;

        case PopupAnimationType.FromLeftElastic:
        case PopupAnimationType.FromLeft:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(-(view.Frame.Width + view.Frame.X), 0);
        break;

        case PopupAnimationType.ZoomIn:
        view.Alpha = 0f;
        view.Transform = CGAffineTransform.MakeScale(0.5f, 0.5f);
        break;

        case PopupAnimationType.ZoomOut:
        view.Alpha = 0f;
        view.Transform = CGAffineTransform.MakeScale(1.5f, 1.5f);
        break;

        case PopupAnimationType.WhirlIn:
        view.Alpha = 0f;
        var rotation1 = CGAffineTransform.MakeRotation((nfloat)(-Math.PI));
        var scale1 = CGAffineTransform.MakeScale(0.3f, 0.3f);
        view.Transform = CGAffineTransform.Multiply(scale1, rotation1);
        break;

        case PopupAnimationType.WhirlIn3:
        view.Alpha = 0f;
        var rotation3 = CGAffineTransform.MakeRotation((nfloat)(-Math.PI * 6));
        var scale3 = CGAffineTransform.MakeScale(0.3f, 0.3f);
        view.Transform = CGAffineTransform.Multiply(scale3, rotation3);
        break;

        case PopupAnimationType.ShrinkHorizontal:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeScale(0.5f, 1f);
        break;

        case PopupAnimationType.ShrinkVertical:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeScale(1f, 0.5f);
        break;

        case PopupAnimationType.ShrinkBoth:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeScale(0.5f, 0.5f);
        break;

        case PopupAnimationType.FlipHorizontal:
        view.Alpha = 0f;
        var transformH = CATransform3D.Identity;
        transformH.M34 = -1.0f / 200.0f; // Stronger perspective for better 3D effect
        transformH = transformH.Rotate((nfloat)(-Math.PI / 2), 0, 1, 0);
        view.Layer.Transform = transformH;
        break;

        case PopupAnimationType.FlipVertical:
        view.Alpha = 0f;
        var transformV = CATransform3D.Identity;
        transformV.M34 = -1.0f / 200.0f; // Stronger perspective for better 3D effect
        transformV = transformV.Rotate((nfloat)(-Math.PI / 2), 1, 0, 0);
        view.Layer.Transform = transformV;
        break;

        default:
        // Fallback to fade
        view.Alpha = 0f;
        break;
        }
    }

    private void AnimateShowContent(UIView contentView, UIView overlayView, PopupAnimationType animationType)
    {
        switch (animationType)
        {
        case PopupAnimationType.Default:
        case PopupAnimationType.Fade:
        case PopupAnimationType.FromBottom:
        case PopupAnimationType.FromTop:
        case PopupAnimationType.FromRight:
        case PopupAnimationType.FromLeft:
        case PopupAnimationType.FromBottomElastic:
        case PopupAnimationType.FromTopElastic:
        case PopupAnimationType.FromLeftElastic:
        case PopupAnimationType.FromRightElastic:
        contentView.Alpha = 1f;
        contentView.Transform = CGAffineTransform.MakeIdentity();
        break;

        case PopupAnimationType.ZoomIn:
        case PopupAnimationType.ZoomOut:
        contentView.Alpha = 1f;
        contentView.Transform = CGAffineTransform.MakeScale(1f, 1f);
        break;

        case PopupAnimationType.WhirlIn:
        // WhirlIn: 1 rotation (180° or π radians)
        contentView.Alpha = 1f;
        contentView.Transform = CGAffineTransform.MakeScale(1f, 1f);

        // Use CABasicAnimation for proper rotation
        var whirlInRotation = CABasicAnimation.FromKeyPath("transform.rotation.z");
        whirlInRotation.From = new NSNumber(-Math.PI);
        whirlInRotation.To = new NSNumber(0);
        whirlInRotation.Duration = 0.25; // Default duration
        whirlInRotation.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
        contentView.Layer.AddAnimation(whirlInRotation, "whirlRotation");
        break;

        case PopupAnimationType.WhirlIn3:
        // WhirlIn3: 6 rotations (1080° or 6π radians)
        contentView.Alpha = 1f;
        contentView.Transform = CGAffineTransform.MakeScale(1f, 1f);

        // Use CABasicAnimation for full 6 rotations
        var whirlIn3Rotation = CABasicAnimation.FromKeyPath("transform.rotation.z");
        whirlIn3Rotation.From = new NSNumber(-Math.PI * 6);
        whirlIn3Rotation.To = new NSNumber(0);
        whirlIn3Rotation.Duration = 0.4; // Extended duration for 6 rotations
        whirlIn3Rotation.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
        contentView.Layer.AddAnimation(whirlIn3Rotation, "whirlRotation");
        break;

        case PopupAnimationType.ShrinkHorizontal:
        case PopupAnimationType.ShrinkVertical:
        case PopupAnimationType.ShrinkBoth:
        // Use keyframe animation for bounce effect
        UIView.AnimateKeyframes(
            duration: 0.3, // Will be overridden by parent animation
            delay: 0,
            options: UIViewKeyframeAnimationOptions.CalculationModeCubic,
            animations: () =>
            {
                UIView.AddKeyframeWithRelativeStartTime(0, 0.7, () =>
                {
                    var overshoot = animationType == PopupAnimationType.ShrinkHorizontal
                        ? CGAffineTransform.MakeScale(1.1f, 1f)
                        : animationType == PopupAnimationType.ShrinkVertical
                            ? CGAffineTransform.MakeScale(1f, 1.1f)
                            : CGAffineTransform.MakeScale(1.1f, 1.1f);
                    contentView.Transform = overshoot;
                });

                UIView.AddKeyframeWithRelativeStartTime(0.7, 0.3, () =>
                {
                    contentView.Transform = CGAffineTransform.MakeIdentity();
                });
            },
            completion: null);
        break;

        case PopupAnimationType.FlipHorizontal:
        case PopupAnimationType.FlipVertical:
        // Only handle alpha - rotation is handled by CABasicAnimation for smooth 3D effect
        contentView.Alpha = 1f;
        // DON'T set Layer.Transform here - CABasicAnimation is handling the rotation
        break;
        }
    }

    private void AnimateHideContent(UIView view, UIView viewContainer, PopupAnimationType animationType)
    {
        // DEBUG: Log the actual state when hide starts
        System.Diagnostics.Debug.WriteLine($"AnimateHideContent - {animationType}:");
        System.Diagnostics.Debug.WriteLine($"  view.Frame: X={view.Frame.X}, Y={view.Frame.Y}, W={view.Frame.Width}, H={view.Frame.Height}");
        System.Diagnostics.Debug.WriteLine($"  view.Bounds: X={view.Bounds.X}, Y={view.Bounds.Y}, W={view.Bounds.Width}, H={view.Bounds.Height}");
        System.Diagnostics.Debug.WriteLine($"  view.Center: X={view.Center.X}, Y={view.Center.Y}");
        System.Diagnostics.Debug.WriteLine($"  view.Transform: {view.Transform}");
        System.Diagnostics.Debug.WriteLine($"  viewContainer.Bounds: W={viewContainer.Bounds.Width}, H={viewContainer.Bounds.Height}");

        switch (animationType)
        {
        case PopupAnimationType.Default:
        case PopupAnimationType.Fade:
        view.Alpha = 0f;
        break;

        case PopupAnimationType.FromBottom:
        case PopupAnimationType.FromBottomElastic:
        view.Transform = CGAffineTransform.MakeTranslation(0, viewContainer.Frame.Height - view.Frame.Y);
        break;

        case PopupAnimationType.FromTop:
        case PopupAnimationType.FromTopElastic:
        view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(0, -(view.Frame.Height + view.Frame.Y));
        break;

        case PopupAnimationType.FromRight:
        case PopupAnimationType.FromRightElastic:
            view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(viewContainer.Frame.Width - view.Frame.X, 0);
        break;

        case PopupAnimationType.FromLeft:
        case PopupAnimationType.FromLeftElastic:
            view.Alpha = 1f;
        view.Transform = CGAffineTransform.MakeTranslation(-(view.Frame.X + view.Frame.Width), 0);
        break;

        case PopupAnimationType.ZoomIn:
        view.Alpha = 0f;
        view.Transform = CGAffineTransform.MakeScale(0.5f, 0.5f);
        break;

        case PopupAnimationType.ZoomOut:
        view.Alpha = 0f;
        view.Transform = CGAffineTransform.MakeScale(1.5f, 1.5f);
        break;

        case PopupAnimationType.WhirlIn:
        case PopupAnimationType.WhirlIn3:
        // Rotation is handled separately via CABasicAnimation in AnimateHideAsync
        // Here we only handle scale and alpha
        view.Alpha = 0f;
        view.Transform = CGAffineTransform.MakeScale(0.3f, 0.3f);
        break;

        case PopupAnimationType.ShrinkHorizontal:
        case PopupAnimationType.ShrinkVertical:
        case PopupAnimationType.ShrinkBoth:
        // Reverse bounce effect with keyframes
        UIView.AnimateKeyframes(
            duration: 0.3,
            delay: 0,
            options: UIViewKeyframeAnimationOptions.CalculationModeCubic,
            animations: () =>
            {
                UIView.AddKeyframeWithRelativeStartTime(0, 0.3, () =>
                {
                    var undershoot = animationType == PopupAnimationType.ShrinkHorizontal
                        ? CGAffineTransform.MakeScale(0.9f, 1f)
                        : animationType == PopupAnimationType.ShrinkVertical
                            ? CGAffineTransform.MakeScale(1f, 0.9f)
                            : CGAffineTransform.MakeScale(0.9f, 0.9f);
                    view.Transform = undershoot;
                });

                UIView.AddKeyframeWithRelativeStartTime(0.3, 0.7, () =>
                {
                    var final = animationType == PopupAnimationType.ShrinkHorizontal
                        ? CGAffineTransform.MakeScale(0.5f, 1f)
                        : animationType == PopupAnimationType.ShrinkVertical
                            ? CGAffineTransform.MakeScale(1f, 0.5f)
                            : CGAffineTransform.MakeScale(0.5f, 0.5f);
                    view.Transform = final;
                });
            },
            completion: null);
        view.Alpha = 0f;
        break;

        case PopupAnimationType.FlipHorizontal:
        case PopupAnimationType.FlipVertical:
        // Only handle alpha - rotation is handled by CABasicAnimation for smooth 3D effect
        view.Alpha = 0f;
        // DON'T set Layer.Transform here - CABasicAnimation is handling the rotation
        break;
        }
    }

    private bool NeedsCenterAnchor(PopupAnimationType animationType)
    {
        return animationType switch
        {
            PopupAnimationType.ZoomIn => true,
            PopupAnimationType.ZoomOut => true,
            PopupAnimationType.WhirlIn => true,
            PopupAnimationType.WhirlIn3 => true,
            PopupAnimationType.ShrinkHorizontal => true,
            PopupAnimationType.ShrinkVertical => true,
            PopupAnimationType.ShrinkBoth => true,
            PopupAnimationType.FlipHorizontal => true,
            PopupAnimationType.FlipVertical => true,
            _ => false
        };
    }

    private bool IsElasticAnimation(PopupAnimationType animationType)
    {
        return animationType switch
        {
            PopupAnimationType.FromBottomElastic => true,
            PopupAnimationType.FromTopElastic => true,
            PopupAnimationType.FromLeftElastic => true,
            PopupAnimationType.FromRightElastic => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the timing parameters for UIViewPropertyAnimator based on easing type.
    /// </summary>
    private IUITimingCurveProvider GetTimingParameters(PopupAnimationEasing easing)
    {
        return easing switch
        {
            PopupAnimationEasing.Linear => new UICubicTimingParameters(UIViewAnimationCurve.Linear),
            PopupAnimationEasing.Decelerate => new UICubicTimingParameters(UIViewAnimationCurve.EaseOut),
            PopupAnimationEasing.Accelerate => new UICubicTimingParameters(UIViewAnimationCurve.EaseIn),
            PopupAnimationEasing.AccelerateDecelerate => new UICubicTimingParameters(UIViewAnimationCurve.EaseInOut),
            // Spring: dampingRatio 0.7 = slightly bouncy, initialVelocity = zero
            PopupAnimationEasing.Spring => new UISpringTimingParameters(0.7f, new CGVector(0, 0)),
            // Elastic: dampingRatio 0.5 = more bouncy for elastic effect, initialVelocity = zero
            PopupAnimationEasing.Elastic => new UISpringTimingParameters(0.5f, new CGVector(0, 0)),
            PopupAnimationEasing.Default => new UICubicTimingParameters(UIViewAnimationCurve.EaseOut),
            _ => new UICubicTimingParameters(UIViewAnimationCurve.EaseOut)
        };
    }

    private void SetAnchorPoint(UIView view, CGPoint anchorPoint)
    {
        var oldOrigin = view.Frame.Location;
        view.Layer.AnchorPoint = anchorPoint;
        var newOrigin = view.Frame.Location;

        var transition = new CGPoint(
            newOrigin.X - oldOrigin.X,
            newOrigin.Y - oldOrigin.Y);

        view.Center = new CGPoint(
            view.Center.X - transition.X,
            view.Center.Y - transition.Y);
    }
}

#endif
