using FastPopups;

namespace SampleApp.Views;

public partial class DrawerMenuPopup : Popup
{
    public DrawerMenuPopup()
    {
        InitializeComponent();
    }

    private void OnMenuItemTapped(object? sender, TappedEventArgs e)
    {
        // Close the drawer when any menu item is tapped
        // In a real app, you would navigate to the selected page here
        Close();
    }

    private void OnEmptySpaceTapped(object? sender, TappedEventArgs e)
    {
        // Close the drawer when empty space is tapped
        Close();
    }
}
