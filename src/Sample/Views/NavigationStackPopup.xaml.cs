using System.ComponentModel;

namespace SampleApp.Views;

public partial class NavigationStackPopup : Popup, INotifyPropertyChanged
{
    private string _title = "";
    private string _description = "";
    private string _stackInfo = "";
    private bool _canShowNext = true;
    private int _level = 1;

    public NavigationStackPopup()
    {
        InitializeComponent();
        BindingContext = this;
        UpdateStackInfo();
    }

    public NavigationStackPopup(int level) : this()
    {
        _level = level;
        Title = $"Popup Level {level}";
        Description = $"This is popup number {level} in the navigation stack.";
        CanShowNext = level < 5; // Limit to 5 levels for demo
        UpdateStackInfo();
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public string StackInfo
    {
        get => _stackInfo;
        set
        {
            _stackInfo = value;
            OnPropertyChanged();
        }
    }

    public bool CanShowNext
    {
        get => _canShowNext;
        set
        {
            _canShowNext = value;
            OnPropertyChanged();
        }
    }

    private void UpdateStackInfo()
    {
        var count = PopupNavigationStack.Instance.Count;
        StackInfo = $"Current stack count: {count} popup(s) in stack";
    }

    private async void OnShowNextClicked(object? sender, EventArgs e)
    {
        if (_level < 5)
        {
            var nextPopup = new NavigationStackPopup(_level + 1);

            // Get the current page to show the popup
            var currentPage = Application.Current?.Windows[0].Page;
            if (currentPage != null)
            {
                await currentPage.ShowPopupAsync(nextPopup);

                // Update stack info when we return
                UpdateStackInfo();
            }
        }
    }

    private void OnCloseThisClicked(object? sender, EventArgs e)
    {
        this.Close($"Closed popup level {_level}");
    }

    private void OnCloseTopClicked(object? sender, EventArgs e)
    {
        var topPopup = PopupNavigationStack.Instance.Peek();
        if (topPopup != null)
        {
            topPopup.Close("Closed via Close Top button");
        }
    }

    private void OnCloseAllClicked(object? sender, EventArgs e)
    {
        PopupNavigationStack.Instance.Clear();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
