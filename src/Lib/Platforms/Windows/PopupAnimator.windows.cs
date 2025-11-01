using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using AppoMobi.Maui.FastPopups;

namespace FastPopups.Platforms.Windows;

/// <summary>
/// Handles Windows-specific popup animations using XAML Storyboards.
/// </summary>
internal class PopupAnimator
{
    private Storyboard? _currentStoryboard;

    /// <summary>
    /// Animates the popup content showing with the specified animation type.
    /// The overlay fade is handled separately and always uses a simple fade animation.
    /// </summary>
    public async Task AnimateShowAsync(
        FrameworkElement contentView,
        FrameworkElement overlayView,
        PopupAnimationType animationType,
        int durationMs,
        PopupAnimationEasing easing,
        CancellationToken cancellationToken = default)
    {
        if (animationType == PopupAnimationType.None)
        {
            // No animation - just ensure visibility
            contentView.Opacity = 1;
            overlayView.Opacity = 1;
            return;
        }

        // Prepare for animation
        var contentStoryboard = CreateShowAnimationStoryboard(contentView, overlayView, animationType, durationMs, easing);
        var overlayStoryboard = CreateOverlayFadeStoryboard(overlayView, toOpacity: 1, durationMs);

        var contentTcs = new TaskCompletionSource<bool>();
        var overlayTcs = new TaskCompletionSource<bool>();

        contentStoryboard.Completed += (s, e) => contentTcs.TrySetResult(true);
        overlayStoryboard.Completed += (s, e) => overlayTcs.TrySetResult(true);

        // Start both animations in parallel
        _currentStoryboard = contentStoryboard;
        contentStoryboard.Begin();
        overlayStoryboard.Begin();

        // Wait for both to complete
        await Task.WhenAll(contentTcs.Task, overlayTcs.Task);

        // Clean up - Don't call Stop() as it can reset animated properties
        // The animations have already completed naturally
        _currentStoryboard = null;
    }

    /// <summary>
    /// Animates the popup content hiding with the specified animation type.
    /// The overlay fade is handled separately and always uses a simple fade animation.
    /// </summary>
    public async Task AnimateHideAsync(
        FrameworkElement contentView,
        FrameworkElement overlayView,
        PopupAnimationType animationType,
        int durationMs,
        PopupAnimationEasing easing,
        CancellationToken cancellationToken = default)
    {
        if (animationType == PopupAnimationType.None)
        {
            // No animation - just hide
            contentView.Opacity = 0;
            overlayView.Opacity = 0;
            return;
        }

        // Prepare for animation (reverse the show animation)
        var contentStoryboard = CreateHideAnimationStoryboard(contentView, overlayView, animationType, durationMs, easing);
        var overlayStoryboard = CreateOverlayFadeStoryboard(overlayView, toOpacity: 0, durationMs);

        var contentTcs = new TaskCompletionSource<bool>();
        var overlayTcs = new TaskCompletionSource<bool>();

        contentStoryboard.Completed += (s, e) =>
        {
            contentTcs.TrySetResult(true);
        };
        overlayStoryboard.Completed += (s, e) =>
        {
            overlayTcs.TrySetResult(true);
        };

        // Start both animations in parallel
        _currentStoryboard = contentStoryboard;
        contentStoryboard.Begin();
        overlayStoryboard.Begin();

        // Wait for both to complete
        await Task.WhenAll(contentTcs.Task, overlayTcs.Task);

        // Clean up - Don't call Stop() as it can reset animated properties
        // The animations have already completed naturally
        _currentStoryboard = null;
    }

    /// <summary>
    /// Cancels any in-progress animation.
    /// </summary>
    public void CancelAnimation()
    {
        _currentStoryboard?.Stop();
        _currentStoryboard = null;
    }

    /// <summary>
    /// Cleans up all animation resources, transforms, and resets elements to default state.
    /// Call this after closing the popup to ensure no leftover transforms or storyboards interfere with input.
    /// </summary>
    public void Cleanup(FrameworkElement? contentView, FrameworkElement? overlayView)
    {
        // Cancel any in-progress animations
        CancelAnimation();

        // Reset content view
        if (contentView != null)
        {
            contentView.Opacity = 1;
            contentView.RenderTransform = null;
            contentView.RenderTransformOrigin = new global::Windows.Foundation.Point(0, 0);
        }

        // Reset overlay view
        if (overlayView != null)
        {
            overlayView.Opacity = 1;
            overlayView.RenderTransform = null;
        }
    }

    /// <summary>
    /// Creates a storyboard for showing the content with the specified animation type.
    /// </summary>
    private Storyboard CreateShowAnimationStoryboard(
        FrameworkElement view,
        FrameworkElement viewContainer,
        PopupAnimationType type,
        int duration,
        PopupAnimationEasing easing)
    {
        var storyboard = new Storyboard();
        var easingFunction = GetEasingFunction(easing, isShow: true);
        var transform = GetOrCreateTransform(view);

        switch (type)
        {
            case PopupAnimationType.Default:
            case PopupAnimationType.Fade:
                // Simple fade in
                view.Opacity = 0;
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", 0, 1, duration, easingFunction));
                break;

            case PopupAnimationType.FromBottom:
                // Slide up from bottom
                view.Opacity = 1;
                transform.TranslateY = 0;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateY", viewContainer.Height -  view.ActualOffset.Y, 0, duration, easingFunction));
                break;

            case PopupAnimationType.FromTop:
                // Slide down from top
                view.Opacity = 1;
                transform.TranslateY = 0;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateY",  - (view.ActualOffset.Y + view.ActualHeight), 0, duration, easingFunction));
                break;

            case PopupAnimationType.FromRight:
                // Slide in from right
                view.Opacity = 1;
                transform.TranslateX = 0;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateX",  viewContainer.ActualWidth - view.ActualOffset.X, 0, duration, easingFunction));
                break;

            case PopupAnimationType.FromLeft:
                // Slide in from left
                view.Opacity = 1;
                transform.TranslateX = 0;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateX", -(view.ActualWidth + view.ActualOffset.X), 0, duration, easingFunction));
                break;

            case PopupAnimationType.ZoomIn:
                // Zoom in from small
                view.Opacity = 0;
                transform.ScaleX = 0.5;
                transform.ScaleY = 0.5;
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", 0, 1, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", 0.5, 1, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", 0.5, 1, duration, easingFunction));
                break;

            case PopupAnimationType.ZoomOut:
                // Zoom in from large
                view.Opacity = 0;
                transform.ScaleX = 1.5;
                transform.ScaleY = 1.5;
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", 0, 1, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", 1.5, 1, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", 1.5, 1, duration, easingFunction));
                break;

            case PopupAnimationType.WhirlIn:
                // 180° rotation + zoom + fade
                view.Opacity = 0;
                transform.Rotation = -180;
                transform.ScaleX = 0.3;
                transform.ScaleY = 0.3;
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", 0, 1, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "Rotation", -180, 0, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", 0.3, 1, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", 0.3, 1, duration, easingFunction));
                break;

            case PopupAnimationType.WhirlIn3:
                // 1080° rotation + zoom + fade (extended duration)
                view.Opacity = 0;
                transform.Rotation = -1080;
                transform.ScaleX = 0.3;
                transform.ScaleY = 0.3;
                var extendedDuration = Math.Max(duration, 400); // Minimum 400ms for dramatic effect
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", 0, 1, extendedDuration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "Rotation", -1080, 0, extendedDuration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", 0.3, 1, extendedDuration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", 0.3, 1, extendedDuration, easingFunction));
                break;

            case PopupAnimationType.ShrinkHorizontal:
                // Bounce effect with keyframe animation
                view.Opacity = 1;
                transform.ScaleX = 1;
                transform.ScaleY = 1;
                var bounceAnimation = CreateShrinkHorizontalAnimation(transform, duration);
                storyboard.Children.Add(bounceAnimation);
                break;

            case PopupAnimationType.FromBottomElastic:
                // Elastic slide from bottom
                view.Opacity = 1;
                transform.TranslateY = view.ActualHeight > 0 ? view.ActualHeight : 600;
                var elasticEasing = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 5 };
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateY", transform.TranslateY, 0, duration, elasticEasing));
                break;

            default:
                // Fallback to fade
                view.Opacity = 0;
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", 0, 1, duration, easingFunction));
                break;
        }

        return storyboard;
    }

    /// <summary>
    /// Creates a storyboard for hiding the content (reverse of show animation).
    /// </summary>
    private Storyboard CreateHideAnimationStoryboard(
        FrameworkElement view,
        FrameworkElement viewContainer,
        PopupAnimationType type,
        int duration,
        PopupAnimationEasing easing)
    {
        var storyboard = new Storyboard();
        var easingFunction = GetEasingFunction(easing, isShow: false);
        var transform = GetOrCreateTransform(view);

        // Get current values
        var currentOpacity = view.Opacity;
        var currentTranslateX = transform.TranslateX;
        var currentTranslateY = transform.TranslateY;
        var currentScaleX = transform.ScaleX;
        var currentScaleY = transform.ScaleY;
        var currentRotation = transform.Rotation;

        switch (type)
        {
            case PopupAnimationType.Default:
            case PopupAnimationType.Fade:
                // Fade out
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", currentOpacity, 0, duration, easingFunction));
                break;

            case PopupAnimationType.FromBottom:
                // Slide down (reverse of slide up)
                var slideDownDistance = view.ActualHeight > 0 ? viewContainer.ActualHeight - view.ActualOffset.Y : 600;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateY", currentTranslateY, slideDownDistance, duration, easingFunction));
                break;

            case PopupAnimationType.FromTop:
                // Slide up (reverse of slide down)
                var slideUpDistance = view.ActualHeight > 0 ? -(view.ActualHeight + view.ActualOffset.Y) : -600;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateY", currentTranslateY, slideUpDistance, duration, easingFunction));
                break;

            case PopupAnimationType.FromRight:
                // Slide right
                var slideRightDistance = view.ActualWidth > 0 ? viewContainer.ActualWidth - view.ActualOffset.X : 600;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateX", currentTranslateX, slideRightDistance, duration, easingFunction));
                break;

            case PopupAnimationType.FromLeft:
                // Slide left
                var slideLeftDistance = view.ActualWidth > 0 ? -(view.ActualOffset.X + view.ActualWidth) : -600;
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "TranslateX", currentTranslateX, slideLeftDistance, duration, easingFunction));
                break;

            case PopupAnimationType.ZoomIn:
                // Zoom out to small (fade + scale down)
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", currentOpacity, 0, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", currentScaleX, 0.5, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", currentScaleY, 0.5, duration, easingFunction));
                break;

            case PopupAnimationType.ZoomOut:
                // Zoom out to large (fade + scale up)
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", currentOpacity, 0, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", currentScaleX, 1.5, duration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", currentScaleY, 1.5, duration, easingFunction));
                break;

            case PopupAnimationType.WhirlIn:
            case PopupAnimationType.WhirlIn3:
                // Reverse rotation + zoom + fade
                var targetRotation = type == PopupAnimationType.WhirlIn3 ? 1080 : 180;
                var whirlDuration = type == PopupAnimationType.WhirlIn3 ? Math.Max(duration, 400) : duration;
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", currentOpacity, 0, whirlDuration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "Rotation", currentRotation, targetRotation, whirlDuration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleX", currentScaleX, 0.3, whirlDuration, easingFunction));
                storyboard.Children.Add(CreateDoubleAnimation(
                    transform, "ScaleY", currentScaleY, 0.3, whirlDuration, easingFunction));
                break;

            case PopupAnimationType.ShrinkHorizontal:
            case PopupAnimationType.FromBottomElastic:
                // For bounce/elastic, just fade out
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", currentOpacity, 0, duration, easingFunction));
                break;

            default:
                // Fallback to fade out
                storyboard.Children.Add(CreateDoubleAnimation(
                    view, "Opacity", currentOpacity, 0, duration, easingFunction));
                break;
        }

        return storyboard;
    }

    /// <summary>
    /// Creates a simple fade storyboard for the overlay (always linear easing).
    /// </summary>
    private Storyboard CreateOverlayFadeStoryboard(FrameworkElement overlay, double toOpacity, int duration)
    {
        var storyboard = new Storyboard();
        var currentOpacity = overlay.Opacity;

        // Overlay always uses linear easing for smooth fade
        var linearEasing = new PowerEase { Power = 1, EasingMode = EasingMode.EaseInOut };

        storyboard.Children.Add(CreateDoubleAnimation(
            overlay, "Opacity", currentOpacity, toOpacity, duration, linearEasing));

        return storyboard;
    }

    /// <summary>
    /// Creates a bounce animation using keyframe animation.
    /// </summary>
    private DoubleAnimationUsingKeyFrames CreateShrinkHorizontalAnimation(CompositeTransform transform, int duration)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, "ScaleX");

        // Keyframes for bounce effect: 1.0 → 1.1 → 1.0
        animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.5 });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration * 0.7)),
            Value = 1.1,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration)),
            Value = 1.0,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        });

        // Also animate ScaleY
        var animationY = new DoubleAnimationUsingKeyFrames();
        Storyboard.SetTarget(animationY, transform);
        Storyboard.SetTargetProperty(animationY, "ScaleY");
        animationY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.5 });
        animationY.KeyFrames.Add(new EasingDoubleKeyFrame {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration * 0.7)),
            Value = 1.1,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
        animationY.KeyFrames.Add(new EasingDoubleKeyFrame {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration)),
            Value = 1.0,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        });

        return animation;
    }

    /// <summary>
    /// Creates a double animation for animating properties.
    /// </summary>
    private DoubleAnimation CreateDoubleAnimation(
        DependencyObject target,
        string property,
        double from,
        double to,
        int duration,
        EasingFunctionBase? easingFunction)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
            EasingFunction = easingFunction
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);

        return animation;
    }

    /// <summary>
    /// Gets or creates a CompositeTransform for the view.
    /// </summary>
    private CompositeTransform GetOrCreateTransform(FrameworkElement view)
    {
        if (view.RenderTransform is not CompositeTransform transform)
        {
            transform = new CompositeTransform();
            view.RenderTransform = transform;
            view.RenderTransformOrigin = new global::Windows.Foundation.Point(0.5, 0.5); // Center pivot
        }
        return transform;
    }

    /// <summary>
    /// Gets the appropriate easing function for the specified easing type.
    /// </summary>
    public EasingFunctionBase GetEasingFunction(PopupAnimationEasing easing, bool isShow)
    {
        return easing switch
        {
            PopupAnimationEasing.Linear => new PowerEase { Power = 1, EasingMode = EasingMode.EaseInOut },
            PopupAnimationEasing.Decelerate => new QuadraticEase { EasingMode = EasingMode.EaseOut },
            PopupAnimationEasing.Accelerate => new QuadraticEase { EasingMode = EasingMode.EaseIn },
            PopupAnimationEasing.AccelerateDecelerate => new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            PopupAnimationEasing.Spring => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 },
            PopupAnimationEasing.Elastic => new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 3 },
            PopupAnimationEasing.Default => isShow
                ? new QuadraticEase { EasingMode = EasingMode.EaseOut }  // Decelerate for show
                : new QuadraticEase { EasingMode = EasingMode.EaseIn },  // Accelerate for hide
            _ => new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
    }
}
