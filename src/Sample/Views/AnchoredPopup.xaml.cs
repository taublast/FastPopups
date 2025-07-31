
namespace SampleApp.Views;

public partial class AnchoredPopup : Popup
{
    public AnchoredPopup()
    {
        InitializeComponent();
    }

    private void OnGotItClicked(object sender, EventArgs e)
    {
        Close();
    }
}