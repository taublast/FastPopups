using Android.Animation;
using Android.Views;
using Android.Views.Animations;
using AppoMobi.Maui.FastPopups;
using Microsoft.Maui.Controls;
using AView = Android.Views.View;

namespace FastPopups.Platforms.Android;

/// <summary>
/// Handles Android-specific popup animations using Android Animator APIs.
/// </summary>
#if ANDROID
public class PopupAnimator
{
    private AnimatorSet? _showAnimatorSet;
    private AnimatorSet? _hideAnimatorSet;
    private bool _isAnimating;
    private bool _hasShownOnce;

    /// <summary>
    /// Prepares the initial state for show animation.
    /// Call this BEFORE the view is shown, to set initial invisible state.
    /// </summary>
    public void PrepareShowAnimation(
        AView contentView,
        AView overlayView,
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
        AView contentView,
        AView overlayView,
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

            // Check if cancellation was already requested
            if (cancellationToken.IsCancellationRequested)
            {
                // Set views to final visible state
                contentView.Alpha = 1f;
                overlayView.Alpha = 1f;
                return;
            }

            // Set pivot point to CENTER of content view for scale/rotation animations
            // This MUST be done after the view has been measured and has proper size
            if (NeedsCenterPivot(animationType))
            {
                contentView.PivotX = contentView.Width / 2f;
                contentView.PivotY = contentView.Height / 2f;
            }

            // Create show animation
            _showAnimatorSet = CreateShowAnimation(contentView, overlayView, animationType, duration, easing);

            // Execute animation
            var tcs = new TaskCompletionSource<bool>();
            _showAnimatorSet.AnimationEnd += (s, e) => tcs.TrySetResult(true);
            _showAnimatorSet.AnimationCancel += (s, e) => tcs.TrySetCanceled();

            // Register cancellation callback
            using (cancellationToken.Register(() =>
            {
                _showAnimatorSet?.Cancel();
                tcs.TrySetCanceled();
            }))
            {
                _showAnimatorSet.Start();

                try
                {
                    await tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    // Animation was canceled - this is expected behavior
                    // Set views to final visible state
                    contentView.Alpha = 1f;
                    overlayView.Alpha = 1f;
                }
            }
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
        AView contentView,
        AView overlayView,
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
            if (animationType == PopupAnimationType.None)
            {
                contentView.Alpha = 0f;
                overlayView.Alpha = 0f;
                return;
            }

            // Check if cancellation was already requested
            if (cancellationToken.IsCancellationRequested)
            {
                contentView.Alpha = 0f;
                overlayView.Alpha = 0f;
                return;
            }

            // Create hide animation
            _hideAnimatorSet = CreateHideAnimation(contentView, overlayView, animationType, duration, easing);

            // Execute animation
            var tcs = new TaskCompletionSource<bool>();
            _hideAnimatorSet.AnimationEnd += (s, e) => tcs.TrySetResult(true);
            _hideAnimatorSet.AnimationCancel += (s, e) => tcs.TrySetCanceled();

            // Register cancellation callback
            using (cancellationToken.Register(() =>
            {
                _hideAnimatorSet?.Cancel();
                tcs.TrySetCanceled();
            }))
            {
                _hideAnimatorSet.Start();

                try
                {
                    await tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    // Animation was canceled - this is expected behavior
                    // Set views to final state
                    contentView.Alpha = 0f;
                    overlayView.Alpha = 0f;
                }
            }
        }
        finally
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// Cleans up animation resources.
    /// </summary>
    public void Cleanup(AView contentView, AView overlayView)
    {
        _showAnimatorSet?.Cancel();
        _hideAnimatorSet?.Cancel();
        _showAnimatorSet?.Dispose();
        _hideAnimatorSet?.Dispose();
        _showAnimatorSet = null;
        _hideAnimatorSet = null;

        // Reset view properties
        contentView.TranslationX = 0;
        contentView.TranslationY = 0;
        contentView.ScaleX = 1f;
        contentView.ScaleY = 1f;
        contentView.Rotation = 0f;
        contentView.RotationX = 0f;
        contentView.RotationY = 0f;
        contentView.Alpha = 1f;

        // Reset pivot to center (for next animation)
        contentView.PivotX = contentView.Width / 2f;
        contentView.PivotY = contentView.Height / 2f;

        overlayView.Alpha = 1f;
    }

    /// <summary>
    /// Determines if an animation type needs center pivot point for scale/rotation.
    /// </summary>
    private bool NeedsCenterPivot(PopupAnimationType animationType)
    {
        return animationType switch
        {
            PopupAnimationType.ZoomIn => true,
            PopupAnimationType.ZoomOut => true,
            PopupAnimationType.Whirl => true,
            PopupAnimationType.BounceInHorizontal => true,
            PopupAnimationType.BounceInVertical => true,
            PopupAnimationType.BounceIn => true,
            PopupAnimationType.BounceOut => true,
            PopupAnimationType.BounceOutHorizontal=> true,
            PopupAnimationType.BounceOutVertical => true,
            PopupAnimationType.FlipHorizontal => true,
            PopupAnimationType.FlipVertical => true,
            _ => false
        };
    }

    public void SetInitialAnimationState(AView view, AView viewContainer, PopupAnimationType animationType)
    {
        // Always start overlay as invisible
        viewContainer.Alpha = 0f;


        // Set content initial state based on animation type
        switch (animationType)
        {
        case PopupAnimationType.Fade:
        view.Alpha = 0f;
        break;

        case PopupAnimationType.SprintBottom:
        case PopupAnimationType.Bottom:
        view.Alpha = 1f;
        view.TranslationY = viewContainer.Height - view.GetY();
        break;

        case PopupAnimationType.SprintTop:
        case PopupAnimationType.Top:
        view.Alpha = 1f;
        view.TranslationY = -(view.GetY() + viewContainer.Height);
        break;

        case PopupAnimationType.SprintRight:
        view.Alpha = 0f;
        view.TranslationX = viewContainer.Width - view.GetX();
        break;

        case PopupAnimationType.Right:
        view.Alpha = 1f;
        view.TranslationX = viewContainer.Width - view.GetX();
        break;

        case PopupAnimationType.SprintLeft:
        case PopupAnimationType.Left:
        view.Alpha = 1f;
        view.TranslationX = -(view.Width + view.GetX());
        break;

        case PopupAnimationType.ZoomIn:
        view.Alpha = 0f;
        view.ScaleX = 0.5f;
        view.ScaleY = 0.5f;
        break;

        case PopupAnimationType.ZoomOut:
        view.Alpha = 0f;
        view.ScaleX = 1.5f;
        view.ScaleY = 1.5f;
        break;

        case PopupAnimationType.Whirl:
        view.Alpha = 0f;
        view.Rotation = -1080f;
        view.ScaleX = 0.3f;
        view.ScaleY = 0.3f;
        break;

        case PopupAnimationType.BounceInHorizontal:
        view.Alpha = 1f;
        view.ScaleX = 0.5f;
        break;

        case PopupAnimationType.BounceInVertical:
        view.Alpha = 1f;
        view.ScaleY = 0.5f;
        break;

        case PopupAnimationType.BounceIn:
        view.Alpha = 1f;
        view.ScaleX = 0.5f;
        view.ScaleY = 0.5f;
        break;

        case PopupAnimationType.BounceOut:
        // BounceOut is opposite of BounceIn - starts large instead of small
        view.Alpha = 0f;
        view.ScaleX = 1.5f;
        view.ScaleY = 1.5f;
        break;

        case PopupAnimationType.BounceOutHorizontal:
        // BounceOutHorizontal - starts wide
        view.Alpha = 0f;
        view.ScaleX = 1.5f;
        view.ScaleY = 1f;
        break;

        case PopupAnimationType.BounceOutVertical:
        // BounceOutVertical - starts tall
        view.Alpha = 0f;
        view.ScaleX = 1f;
        view.ScaleY = 1.5f;
        break;

        case PopupAnimationType.FlipHorizontal:
        view.Alpha = 0f;
        view.RotationY = -90f;
        break;

        case PopupAnimationType.FlipVertical:
        view.Alpha = 0f;
        view.RotationX = -90f;
        break;

        default:
        // Fallback to fade
        view.Alpha = 0f;
        break;
        }
    }

    private AnimatorSet CreateShowAnimation(
        AView contentView,
        AView overlayView,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing)
    {
        var animatorSet = new AnimatorSet();
        var animators = new List<Animator>();

        // Always fade in overlay with linear interpolation
        var overlayAnimator = ObjectAnimator.OfFloat(overlayView, "alpha", 0f, 1f);
        overlayAnimator.SetDuration(duration);
        overlayAnimator.SetInterpolator(new LinearInterpolator());
        animators.Add(overlayAnimator);

        // Create content animation based on type
        var contentAnimators = CreateShowContentAnimation(contentView, animationType, duration, easing);
        animators.AddRange(contentAnimators);

        animatorSet.PlayTogether(animators.ToArray());
        return animatorSet;
    }

    private List<Animator> CreateShowContentAnimation(
        AView contentView,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing)
    {
        var animators = new List<Animator>();
        var interpolator = GetInterpolator(easing, isShow: true);

        // Adjust duration for WhirlIn3
        if (animationType == PopupAnimationType.Whirl)
        {
            duration = Math.Max(duration, 400);
        }

        switch (animationType)
        {
        case PopupAnimationType.Fade:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        break;

        case PopupAnimationType.Bottom:
        animators.Add(CreatePropertyAnimator(contentView, "translationY", contentView.TranslationY, 0f, duration, interpolator));
        break;

        case PopupAnimationType.Top:
        animators.Add(CreatePropertyAnimator(contentView, "translationY", contentView.TranslationY, 0f, duration, interpolator));
        break;

        case PopupAnimationType.Right:
        animators.Add(CreatePropertyAnimator(contentView, "translationX", contentView.TranslationX, 0f, duration, interpolator));
        break;

        case PopupAnimationType.Left:
        animators.Add(CreatePropertyAnimator(contentView, "translationX", contentView.TranslationX, 0f, duration, interpolator));
        break;

        case PopupAnimationType.ZoomIn:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "scaleX", 0.5f, 1f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "scaleY", 0.5f, 1f, duration, interpolator));
        break;

        case PopupAnimationType.ZoomOut:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "scaleX", 1.5f, 1f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "scaleY", 1.5f, 1f, duration, interpolator));
        break;

        case PopupAnimationType.Whirl:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "rotation", contentView.Rotation, 0f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "scaleX", 0.3f, 1f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(contentView, "scaleY", 0.3f, 1f, duration, interpolator));
        break;

        case PopupAnimationType.BounceInHorizontal:
        animators.Add(CreateBounceAnimator(contentView, "scaleX", 0.5f, 1.1f, 1f, duration));
        break;

        case PopupAnimationType.BounceInVertical:
        animators.Add(CreateBounceAnimator(contentView, "scaleY", 0.5f, 1.1f, 1f, duration));
        break;

        case PopupAnimationType.BounceIn:
        animators.Add(CreateBounceAnimator(contentView, "scaleX", 0.5f, 1.1f, 1f, duration));
        animators.Add(CreateBounceAnimator(contentView, "scaleY", 0.5f, 1.1f, 1f, duration));
        break;

        case PopupAnimationType.BounceOut:
        // BounceOut is opposite of BounceIn: 1.5 → 0.9 → 1.0
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        animators.Add(CreateReverseBounceAnimator(contentView, "scaleX", 1.5f, 0.9f, 1f, duration));
        animators.Add(CreateReverseBounceAnimator(contentView, "scaleY", 1.5f, 0.9f, 1f, duration));
        break;

        case PopupAnimationType.BounceOutHorizontal:
        // BounceOutHorizontal: X: 1.5 → 0.9 → 1.0
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        animators.Add(CreateReverseBounceAnimator(contentView, "scaleX", 1.5f, 0.9f, 1f, duration));
        break;

        case PopupAnimationType.BounceOutVertical:
        // BounceOutVertical: Y: 1.5 → 0.9 → 1.0
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        animators.Add(CreateReverseBounceAnimator(contentView, "scaleY", 1.5f, 0.9f, 1f, duration));
        break;

        case PopupAnimationType.SprintBottom:
        case PopupAnimationType.SprintTop:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0.5f, 1f, duration, interpolator));
        var elasticInterpolator = new OvershootInterpolator(2f);
        animators.Add(CreatePropertyAnimator(contentView, "translationY", contentView.TranslationY, 0f, duration, elasticInterpolator));
        break;

        case PopupAnimationType.SprintLeft:
        case PopupAnimationType.SprintRight:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0.5f, 1f, duration, interpolator));
        var elasticInterpolatorX = new OvershootInterpolator(2f);
        animators.Add(CreatePropertyAnimator(contentView, "translationX", contentView.TranslationX, 0f, duration, elasticInterpolatorX));
        break;

        case PopupAnimationType.FlipHorizontal:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        // Use hardcoded DecelerateInterpolator for smooth flip effect
        animators.Add(CreatePropertyAnimator(contentView, "rotationY", -90f, 0f, duration, new DecelerateInterpolator()));
        break;

        case PopupAnimationType.FlipVertical:
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        // Use hardcoded DecelerateInterpolator for smooth flip effect
        animators.Add(CreatePropertyAnimator(contentView, "rotationX", -90f, 0f, duration, new DecelerateInterpolator()));
        break;

        default:
        // Fallback to fade
        animators.Add(CreatePropertyAnimator(contentView, "alpha", 0f, 1f, duration, interpolator));
        break;
        }

        return animators;
    }

    private AnimatorSet CreateHideAnimation(
        AView contentView,
        AView overlayView,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing)
    {
        var animatorSet = new AnimatorSet();
        var animators = new List<Animator>();

        // Always fade out overlay with linear interpolation
        var overlayAnimator = ObjectAnimator.OfFloat(overlayView, "alpha", overlayView.Alpha, 0f);
        overlayAnimator.SetDuration(duration);
        overlayAnimator.SetInterpolator(new LinearInterpolator());
        animators.Add(overlayAnimator);

        // Create content animation based on type
        var contentAnimators = CreateHideContentAnimation(contentView, overlayView, animationType, duration, easing);
        animators.AddRange(contentAnimators);

        animatorSet.PlayTogether(animators.ToArray());
        return animatorSet;
    }

    private List<Animator> CreateHideContentAnimation(
        AView view,
        AView viewContainer,
        PopupAnimationType animationType,
        int duration,
        PopupAnimationEasing easing)
    {
        var animators = new List<Animator>();
        var interpolator = GetInterpolator(easing, isShow: false);

        // Adjust duration for WhirlIn3
        if (animationType == PopupAnimationType.Whirl)
        {
            duration = Math.Max(duration, 400);
        }

        var currentAlpha = view.Alpha;
        var currentTranslationX = view.TranslationX;
        var currentTranslationY = view.TranslationY;
        var currentScaleX = view.ScaleX;
        var currentScaleY = view.ScaleY;
        var currentRotation = view.Rotation;
        var currentRotationX = view.RotationX;
        var currentRotationY = view.RotationY;

        switch (animationType)
        {
        case PopupAnimationType.Fade:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.Bottom:
        var slideDownDistance = viewContainer.Height - view.GetY();
        animators.Add(CreatePropertyAnimator(view, "translationY", currentTranslationY, slideDownDistance, duration, interpolator));
        break;

        case PopupAnimationType.Top:
        var slideUpDistance = -(view.Height + view.GetY());
        animators.Add(CreatePropertyAnimator(view, "translationY", currentTranslationY, slideUpDistance, duration, interpolator));
        break;

        case PopupAnimationType.Left:
        var slideLeftDistance = -(view.GetX() + view.Width);
        animators.Add(CreatePropertyAnimator(view, "translationX", currentTranslationX, slideLeftDistance, duration, interpolator));
        break;

        case PopupAnimationType.Right:
        var slideRightDistance = viewContainer.Width - view.GetX();
        animators.Add(CreatePropertyAnimator(view, "translationX", currentTranslationX, slideRightDistance, duration, interpolator));
        break;

        case PopupAnimationType.SprintBottom:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0.5f, duration, interpolator));
        var elasticDownDistance = viewContainer.Height - view.GetY();
        var anticipateInterpolator = new AnticipateInterpolator(2f);
        animators.Add(CreatePropertyAnimator(view, "translationY", currentTranslationY, elasticDownDistance, duration, anticipateInterpolator));
        break;

        case PopupAnimationType.SprintTop:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0.5f, duration, interpolator));
        var elasticUpDistance = -(view.Height + view.GetY());
        var anticipateInterpolatorUp = new AnticipateInterpolator(2f);
        animators.Add(CreatePropertyAnimator(view, "translationY", currentTranslationY, elasticUpDistance, duration, anticipateInterpolatorUp));
        break;

        case PopupAnimationType.SprintLeft:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0.5f, duration, interpolator));
        var elasticLeftDistance = -(view.GetX() + view.Width);
        var anticipateInterpolatorLeft = new AnticipateInterpolator(2f);
        animators.Add(CreatePropertyAnimator(view, "translationX", currentTranslationX, elasticLeftDistance, duration, anticipateInterpolatorLeft));
        break;

        case PopupAnimationType.SprintRight:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0.5f, duration, interpolator));
        var elasticRightDistance = viewContainer.Width - view.GetX();
        var anticipateInterpolatorRight = new AnticipateInterpolator(2f);
        animators.Add(CreatePropertyAnimator(view, "translationX", currentTranslationX, elasticRightDistance, duration, anticipateInterpolatorRight));
        break;

        case PopupAnimationType.ZoomIn:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "scaleX", currentScaleX, 0.5f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "scaleY", currentScaleY, 0.5f, duration, interpolator));
        break;

        case PopupAnimationType.ZoomOut:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "scaleX", currentScaleX, 1.5f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "scaleY", currentScaleY, 1.5f, duration, interpolator));
        break;

        case PopupAnimationType.Whirl:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "rotation", currentRotation, 1080f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "scaleX", currentScaleX, 0.3f, duration, interpolator));
        animators.Add(CreatePropertyAnimator(view, "scaleY", currentScaleY, 0.3f, duration, interpolator));
        break;

        case PopupAnimationType.BounceInHorizontal:
        animators.Add(CreateReverseBounceAnimator(view, "scaleX", 1f, 0.9f, 0.5f, duration));
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.BounceInVertical:
        animators.Add(CreateReverseBounceAnimator(view, "scaleY", 1f, 0.9f, 0.5f, duration));
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.BounceIn:
        animators.Add(CreateReverseBounceAnimator(view, "scaleX", 1f, 0.9f, 0.5f, duration));
        animators.Add(CreateReverseBounceAnimator(view, "scaleY", 1f, 0.9f, 0.5f, duration));
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.BounceOut:
        // BounceOut is opposite of BounceIn hide: 1.0 → 1.1 → 1.5
        animators.Add(CreateBounceAnimator(view, "scaleX", 1f, 1.1f, 1.5f, duration));
        animators.Add(CreateBounceAnimator(view, "scaleY", 1f, 1.1f, 1.5f, duration));
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.BounceOutHorizontal:
        // BounceOutHorizontal hide: X: 1.0 → 1.1 → 1.5
        animators.Add(CreateBounceAnimator(view, "scaleX", 1f, 1.1f, 1.5f, duration));
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.BounceOutVertical:
        // BounceOutVertical hide: Y: 1.0 → 1.1 → 1.5
        animators.Add(CreateBounceAnimator(view, "scaleY", 1f, 1.1f, 1.5f, duration));
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;

        case PopupAnimationType.FlipHorizontal:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        // Use hardcoded AccelerateInterpolator for smooth flip effect on hide
        animators.Add(CreatePropertyAnimator(view, "rotationY", currentRotationY, 90f, duration, new AccelerateInterpolator()));
        break;

        case PopupAnimationType.FlipVertical:
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        // Use hardcoded AccelerateInterpolator for smooth flip effect on hide
        animators.Add(CreatePropertyAnimator(view, "rotationX", currentRotationX, 90f, duration, new AccelerateInterpolator()));
        break;

        default:
        // Fallback to fade
        animators.Add(CreatePropertyAnimator(view, "alpha", currentAlpha, 0f, duration, interpolator));
        break;
        }

        return animators;
    }

    private ObjectAnimator CreatePropertyAnimator(
        AView view,
        string propertyName,
        float fromValue,
        float toValue,
        int duration,
        ITimeInterpolator? interpolator)
    {
        var animator = ObjectAnimator.OfFloat(view, propertyName, fromValue, toValue);
        animator.SetDuration(duration);
        if (interpolator != null)
        {
            animator.SetInterpolator(interpolator);
        }
        return animator;
    }

    private ObjectAnimator CreateBounceAnimator(
        AView view,
        string propertyName,
        float startValue,
        float overshootValue,
        float endValue,
        int duration)
    {
        // Create keyframe animation: startValue -> overshootValue (70%) -> endValue (100%)
        var keyframes = new Keyframe[3];
        keyframes[0] = Keyframe.OfFloat(0f, startValue);
        keyframes[1] = Keyframe.OfFloat(0.7f, overshootValue);
        keyframes[2] = Keyframe.OfFloat(1f, endValue);

        var propertyValuesHolder = PropertyValuesHolder.OfKeyframe(propertyName, keyframes);
        var animator = ObjectAnimator.OfPropertyValuesHolder(view, propertyValuesHolder);
        animator.SetDuration(duration);
        // Use overall decelerate interpolator for smooth bounce
        animator.SetInterpolator(new DecelerateInterpolator(1.5f));

        return animator;
    }

    private ObjectAnimator CreateReverseBounceAnimator(
        AView view,
        string propertyName,
        float startValue,
        float underValue,
        float endValue,
        int duration)
    {
        // Create keyframe animation: startValue -> underValue (30%) -> endValue (100%)
        var keyframes = new Keyframe[3];
        keyframes[0] = Keyframe.OfFloat(0f, startValue);
        keyframes[1] = Keyframe.OfFloat(0.3f, underValue);
        keyframes[2] = Keyframe.OfFloat(1f, endValue);

        var propertyValuesHolder = PropertyValuesHolder.OfKeyframe(propertyName, keyframes);
        var animator = ObjectAnimator.OfPropertyValuesHolder(view, propertyValuesHolder);
        animator.SetDuration(duration);
        // Use overall accelerate interpolator for smooth shrink
        animator.SetInterpolator(new AccelerateInterpolator(1.5f));

        return animator;
    }

    private ITimeInterpolator? GetInterpolator(PopupAnimationEasing easing, bool isShow)
    {
        return easing switch
        {
            PopupAnimationEasing.Default => isShow
                ? new DecelerateInterpolator()
                : new AccelerateInterpolator(),
            PopupAnimationEasing.Linear => new LinearInterpolator(),
            PopupAnimationEasing.Decelerate => new DecelerateInterpolator(),
            PopupAnimationEasing.Accelerate => new AccelerateInterpolator(),
            PopupAnimationEasing.AccelerateDecelerate => new AccelerateDecelerateInterpolator(),
            PopupAnimationEasing.Spring => new OvershootInterpolator(1.5f),
            PopupAnimationEasing.Elastic => new OvershootInterpolator(2f),
            _ => isShow ? new DecelerateInterpolator() : new AccelerateInterpolator()
        };
    }
}
#endif
