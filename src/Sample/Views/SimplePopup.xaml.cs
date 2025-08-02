
namespace SampleApp.Views;

public partial class SimplePopup : Popup
{
    public SimplePopup()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }

 

}