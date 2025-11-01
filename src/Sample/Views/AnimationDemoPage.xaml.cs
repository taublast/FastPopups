using AppoMobi.Maui.FastPopups;

namespace SampleApp.Views;

public partial class AnimationDemoPage : ContentPage
{
    public AnimationDemoPage()
    {
        InitializeComponent();

        // Set defaults
        AnimationTypePicker.SelectedIndex = 2; // Fade
        EasingPicker.SelectedIndex = 0; // Default
        UpdateInfoLabel();
    }

    private void OnAnimationTypeChanged(object? sender, EventArgs e)
    {
        UpdateInfoLabel();
    }

    private void OnDurationChanged(object? sender, ValueChangedEventArgs e)
    {
        if (DurationLabel != null)
        {
            var duration = (int)e.NewValue;
            DurationLabel.Text = $"{duration} ms";
        }
    }

    private void UpdateInfoLabel()
    {
        if (AnimationTypePicker.SelectedIndex < 0)
        {
            InfoLabel.Text = "";
            return;
        }

        var animationType = GetSelectedAnimationType();
        InfoLabel.Text = GetAnimationInfo(animationType);
    }

    private PopupAnimationType GetSelectedAnimationType()
    {
        return AnimationTypePicker.SelectedIndex switch
        {
            0 => PopupAnimationType.Default,
            1 => PopupAnimationType.None,
            2 => PopupAnimationType.Fade,
            3 => PopupAnimationType.FromBottom,
            4 => PopupAnimationType.FromTop,
            5 => PopupAnimationType.FromRight,
            6 => PopupAnimationType.FromLeft,
            7 => PopupAnimationType.ZoomIn,
            8 => PopupAnimationType.ZoomOut,
            9 => PopupAnimationType.WhirlIn,
            10 => PopupAnimationType.WhirlIn3,
            11 => PopupAnimationType.ShrinkHorizontal,
            12 => PopupAnimationType.ShrinkVertical,
            13 => PopupAnimationType.ShrinkBoth,
            14 => PopupAnimationType.FromBottomElastic,
            15 => PopupAnimationType.FromTopElastic,
            16 => PopupAnimationType.FromLeftElastic,
            17 => PopupAnimationType.FromRightElastic,
            18 => PopupAnimationType.FlipHorizontal,
            19 => PopupAnimationType.FlipVertical,
            _ => PopupAnimationType.Fade
        };
    }

    private PopupAnimationEasing GetSelectedEasing()
    {
        return EasingPicker.SelectedIndex switch
        {
            0 => PopupAnimationEasing.Default,
            1 => PopupAnimationEasing.Linear,
            2 => PopupAnimationEasing.Decelerate,
            3 => PopupAnimationEasing.Accelerate,
            4 => PopupAnimationEasing.AccelerateDecelerate,
            5 => PopupAnimationEasing.Spring,
            6 => PopupAnimationEasing.Elastic,
            _ => PopupAnimationEasing.Default
        };
    }

    private string GetAnimationInfo(PopupAnimationType type)
    {
        return type switch
        {
            PopupAnimationType.Default => "Uses platform-specific default animation. The overlay fades while content animates.",
            PopupAnimationType.None => "Displays instantly without any animation. Useful for quick interactions.",
            PopupAnimationType.Fade => "Simple fade in/out effect. Both overlay and content fade simultaneously.",
            PopupAnimationType.FromBottom => "Slides up from bottom of screen. Most common animation for modal dialogs.",
            PopupAnimationType.FromTop => "Slides down from top of screen. Good for notifications or dropdown menus.",
            PopupAnimationType.FromRight => "Slides in from right side. Similar to iOS navigation push.",
            PopupAnimationType.FromLeft => "Slides in from left side. Good for side panels or back navigation.",
            PopupAnimationType.ZoomIn => "Zooms in from center (scale 0.5→1.0). Creates focus on the popup content.",
            PopupAnimationType.ZoomOut => "Zooms out to center (scale 1.5→1.0). Dramatic entrance effect.",
            PopupAnimationType.WhirlIn => "Rotates 180° while zooming in. Fun and attention-grabbing.",
            PopupAnimationType.WhirlIn3 => "Rotates 1080° (3 full rotations) while zooming in. Very dramatic effect with extended duration.",
            PopupAnimationType.ShrinkHorizontal => "Horizontal bounce with scale overshoot (0.5→1.1→1.0). Playful width squash effect.",
            PopupAnimationType.ShrinkVertical => "Vertical bounce with scale overshoot (0.5→1.1→1.0). Playful height squash effect.",
            PopupAnimationType.ShrinkBoth => "Combined bounce on both axes (0.5→1.1→1.0). Full squash and stretch effect.",
            PopupAnimationType.FromBottomElastic => "Slides from bottom with spring physics. Natural and satisfying motion.",
            PopupAnimationType.FromTopElastic => "Slides from top with spring physics. Bouncy dropdown effect.",
            PopupAnimationType.FromLeftElastic => "Slides from left with spring physics. Elastic side panel entrance.",
            PopupAnimationType.FromRightElastic => "Slides from right with spring physics. Elastic side panel entrance.",
            PopupAnimationType.FlipHorizontal => "3D horizontal flip like a card turning. Rotates 90° around Y-axis.",
            PopupAnimationType.FlipVertical => "3D vertical flip like a trapdoor. Rotates 90° around X-axis.",
            _ => "No information available for this animation type."
        };
    }

    private async void OnShowPopupClicked(object? sender, EventArgs e)
    {
        var animationType = GetSelectedAnimationType();
        var easing = GetSelectedEasing();
        var duration = (int)DurationSlider.Value;

        var popup = new AnimationTestPopup(animationType,duration,easing);

        await this.ShowPopupAsync(popup);
    }

    private async void OnShowAllAnimationsClicked(object? sender, EventArgs e)
    {
        var animations = new[]
        {
            PopupAnimationType.Fade,
            PopupAnimationType.FromBottom,
            PopupAnimationType.FromBottomElastic,
            PopupAnimationType.FromTop,
            PopupAnimationType.FromTopElastic,
            PopupAnimationType.FromRight,
            PopupAnimationType.FromRightElastic,
            PopupAnimationType.FromLeft,
            PopupAnimationType.FromLeftElastic,
            PopupAnimationType.ZoomIn,
            PopupAnimationType.ZoomOut,
            PopupAnimationType.ShrinkHorizontal,
            PopupAnimationType.ShrinkVertical,
            PopupAnimationType.ShrinkBoth,
            PopupAnimationType.FlipHorizontal,
            PopupAnimationType.FlipVertical,
            PopupAnimationType.WhirlIn,
            PopupAnimationType.WhirlIn3,
        };

        foreach (var animationType in animations)
        {
            var time = animationType == PopupAnimationType.WhirlIn3 ? 400 : 300;

            var popup = new AnimationTestPopup(animationType, time, PopupAnimationEasing.Default);

            this.ShowPopup(popup);

            await Task.Delay(time + 1500);

            popup.Close();

            await Task.Delay(time + 500);
        }
    }
}
