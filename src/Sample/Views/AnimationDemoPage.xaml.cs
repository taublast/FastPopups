using AppoMobi.Maui.FastPopups;
using System.ComponentModel;
using System.Reflection;

namespace SampleApp.Views;

public partial class AnimationDemoPage : ContentPage
{
    public AnimationDemoPage()
    {
        InitializeComponent();

        // Populate animation type picker from enum
        foreach (PopupAnimationType animType in Enum.GetValues(typeof(PopupAnimationType)))
        {
            AnimationTypePicker.Items.Add(animType.ToString());
        }

        // Populate easing picker from enum
        foreach (PopupAnimationEasing easingType in Enum.GetValues(typeof(PopupAnimationEasing)))
        {
            EasingPicker.Items.Add(easingType.ToString());
        }

        // Set defaults
        AnimationTypePicker.SelectedIndex = Array.IndexOf(Enum.GetValues(typeof(PopupAnimationType)), PopupAnimationType.Fade);
        EasingPicker.SelectedIndex = Array.IndexOf(Enum.GetValues(typeof(PopupAnimationEasing)), PopupAnimationEasing.Default);
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
        if (AnimationTypePicker.SelectedIndex < 0)
            return PopupAnimationType.Fade;

        var selectedText = AnimationTypePicker.Items[AnimationTypePicker.SelectedIndex];
        return Enum.Parse<PopupAnimationType>(selectedText);
    }

    private PopupAnimationEasing GetSelectedEasing()
    {
        if (EasingPicker.SelectedIndex < 0)
            return PopupAnimationEasing.Default;

        var selectedText = EasingPicker.Items[EasingPicker.SelectedIndex];
        return Enum.Parse<PopupAnimationEasing>(selectedText);
    }

    private string GetAnimationInfo(PopupAnimationType type)
    {
        // Get description from [Description] attribute via reflection
        var fieldInfo = type.GetType().GetField(type.ToString());
        var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();

        return descriptionAttribute?.Description ?? "No information available for this animation type.";
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
        // Automatically get all animation types from enum, excluding None
        var animations = Enum.GetValues(typeof(PopupAnimationType))
            .Cast<PopupAnimationType>()
            .Where(a => a != PopupAnimationType.None)
            .ToArray();

        foreach (var animationType in animations)
        {
            var time = animationType == PopupAnimationType.Whirl ? 400 : 300;

            var popup = new AnimationTestPopup(animationType, time, PopupAnimationEasing.Default);

            this.ShowPopup(popup);

            await Task.Delay(time + 1500);

            popup.Close();

            await Task.Delay(time + 500);
        }
    }
}
