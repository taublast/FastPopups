using Microsoft.Maui.Controls.Shapes;
using SampleApp.Views;

namespace SampleApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnSimplePopupClicked(object? sender, EventArgs e)
    {
        var popup = new SimplePopup();
        this.ShowPopup(popup);
    }

    private void OnFillPopupClicked(object? sender, EventArgs e)
    {
        var popup = new FillPopup();
        this.ShowPopup(popup);
    }

    private async void OnAnchoredPopupClicked(object? sender, EventArgs e)
    {
        var popup = new AnchoredPopup
        {
            Anchor = AnchoredButton
        };
        await this.ShowPopupAsync(popup);
    }

    private async void OnCustomSizePopupClicked(object? sender, EventArgs e)
    {
        Popup popup = null!;
        popup = new Popup
        {
            IsFullScreen = false,
            WidthRequest = 400,
            BackgroundColor = Colors.Red.WithAlpha(0.85f),
            CloseWhenBackgroundIsClicked = true,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.End,
            Content = new Border
            {
                BackgroundColor = Colors.LightPink,
                StrokeThickness = 3,
                Stroke = Colors.Purple,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Content = new StackLayout
                {
                    Padding = 30,
                    Spacing = 20,
                    Children =
                    {
                        new Label
                        {
                            Text = "Custom Positioned Popup",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.Purple
                        },
                        new Label
                        {
                            Text =
                                "This popup demonstrates custom sizing and positioning. Will close if you tap anywhere on the screen!",
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center,
                            TextColor = Colors.DarkSlateGray
                        },
                        new Button
                        {
                            Text = "Toggle IsFullScreen",
                            BackgroundColor = Colors.Orange,
                            TextColor = Colors.White,
                            Command = new Command(() =>
                            {
                                popup.IsFullScreen = !popup.IsFullScreen;
                            })
                        },
                        new Button
                        {
                            Text = "Awesome!",
                            BackgroundColor = Colors.Purple,
                            TextColor = Colors.White,
                            Command = new Command(() => popup.Close())
                        }
                    }
                }
            }
        };

        await this.ShowPopupAsync(popup);
    }

    // Navigation Stack Examples
    private async void OnNavigationStackDemoClicked(object? sender, EventArgs e)
    {
        var popup = new Views.NavigationStackPopup(1);
        await this.ShowPopupAsync(popup);
    }

    // Animation Demo
    private async void OnAnimationDemoClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.AnimationDemoPage());
    }

    // Drawer Menu Demo
    private async void OnDrawerMenuDemoClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.DrawerMenuPage());
    }

}