
namespace SampleApp.Views;

public partial class FillPopup : Popup
{
    public FillPopup()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }


    private void OnInsetsClicked(object? sender, EventArgs e)
    {
        IgnoreSafeArea = !IgnoreSafeArea;
    }
}