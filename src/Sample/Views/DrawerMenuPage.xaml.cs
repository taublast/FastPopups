using FastPopups;

namespace SampleApp.Views;

public partial class DrawerMenuPage : ContentPage
{
    public DrawerMenuPage()
    {
        InitializeComponent();
    }

    private async void OnMenuButtonClicked(object? sender, EventArgs e)
    {
        var drawerPopup = new DrawerMenuPopup();
        await this.ShowPopupAsync(drawerPopup);
    }
}
