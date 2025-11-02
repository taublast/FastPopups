using AppoMobi.Maui.FastPopups;
using System.ComponentModel;
using System.Reflection;
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
        // Get description from [Description] attribute via reflection
        var fieldInfo = type.GetType().GetField(type.ToString());
        var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();

        return descriptionAttribute?.Description ?? "No description available";
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
