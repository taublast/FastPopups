using Microsoft.Maui.Controls.Shapes;
using SampleApp.Views;

namespace SampleApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnSimplePopupClicked(object? sender, EventArgs e)
	{
		var popup = new SimplePopup();
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

	private async void OnCenteredPopupClicked(object? sender, EventArgs e)
	{
		Popup popup = null!;
		popup = new Popup
		{
			HeightRequest = 250,
			WidthRequest = 350,
            BackgroundColor = Colors.Black.WithAlpha(0.6f),
			CloseWhenBackgroundIsClicked = true,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Content = new Border
			{
				BackgroundColor = Colors.White,
				StrokeThickness = 2,
				Stroke = Colors.Green,
				StrokeShape = new RoundRectangle { CornerRadius = 15 },
				Content = new StackLayout
				{
					Padding = 25,
					Spacing = 15,
					Children =
					{
						new Label
						{
							Text = "Centered Popup",
							FontSize = 20,
							FontAttributes = FontAttributes.Bold,
							HorizontalOptions = LayoutOptions.Center,
							TextColor = Colors.Green
						},
						new Label
						{
							Text = "This popup is perfectly centered on the screen with a custom size and overlay color.",
							HorizontalOptions = LayoutOptions.Center,
							HorizontalTextAlignment = TextAlignment.Center,
							TextColor = Colors.Gray
						},
						new Button
						{
							Text = "Close",
							BackgroundColor = Colors.Green,
							TextColor = Colors.White,
							Command = new Command(() => popup.Close())
						}
					}
				}
			}
		};

		await this.ShowPopupAsync(popup);
	}

	private async void OnCustomSizePopupClicked(object? sender, EventArgs e)
	{
		Popup popup = null!;
		popup = new Popup
		{
			HeightRequest = 300,
			WidthRequest = 400,
            BackgroundColor = Colors.Purple.WithAlpha(0.4f),
			CloseWhenBackgroundIsClicked = true,
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.Start,
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
							Text = "This popup demonstrates custom sizing and positioning. It's positioned at the top-left with a purple theme. Will close if you tap anywhere on the screen!",
							HorizontalOptions = LayoutOptions.Center,
							HorizontalTextAlignment = TextAlignment.Center,
							TextColor = Colors.DarkSlateGray
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
}
