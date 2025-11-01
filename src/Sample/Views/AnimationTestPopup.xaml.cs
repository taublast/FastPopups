using AppoMobi.Maui.FastPopups;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SampleApp.Views;

public partial class AnimationTestPopup : Popup, INotifyPropertyChanged
{
    private string _animationName = "";
    private string _animationDescription = "";
    private string _animationDetails = "";

    public AnimationTestPopup(PopupAnimationType animationType, int duration, PopupAnimationEasing easing)
    {
        AnimationType = animationType;
        AnimationDuration = duration;
        AnimationEasing = easing;

        AnimationName = GetAnimationName(AnimationType);
        AnimationDescription = GetAnimationDescription(AnimationType);
        AnimationDetails = $"Duration: {AnimationDuration}ms | Easing: {AnimationEasing}";

        InitializeComponent();

        BindingContext = this;
    }


    public string AnimationName
    {
        get => _animationName;
        set
        {
            _animationName = value;
            OnPropertyChanged();
        }
    }

    public string AnimationDescription
    {
        get => _animationDescription;
        set
        {
            _animationDescription = value;
            OnPropertyChanged();
        }
    }

    public string AnimationDetails
    {
        get => _animationDetails;
        set
        {
            _animationDetails = value;
            OnPropertyChanged();
        }
    }

 
    private string GetAnimationName(PopupAnimationType type)
    {
        return $"{type}";
    }

    private string GetAnimationDescription(PopupAnimationType type)
    {
        return type switch
        {
            PopupAnimationType.Default => "Platform-specific default animation",
            PopupAnimationType.None => "Instant display without animation",
            PopupAnimationType.Fade => "Smooth fade in/out effect",
            PopupAnimationType.FromBottom => "Slides up from bottom (most common for modals)",
            PopupAnimationType.FromTop => "Slides down from top",
            PopupAnimationType.FromRight => "Slides in from right side",
            PopupAnimationType.FromLeft => "Slides in from left side",
            PopupAnimationType.ZoomIn => "Zooms in from center (scale 0.5→1.0)",
            PopupAnimationType.ZoomOut => "Zooms out to center (scale 1.5→1.0)",
            PopupAnimationType.WhirlIn => "180° rotation with zoom effect",
            PopupAnimationType.WhirlIn3 => "1080° rotation with zoom (extended duration)",
            PopupAnimationType.ShrinkHorizontal => "Horizontal scale overshoot",
            PopupAnimationType.FromBottomElastic => "Slide from bottom with spring physics",
            _ => "No description available"
        };
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        Close();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
