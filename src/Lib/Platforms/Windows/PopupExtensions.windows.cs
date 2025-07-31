using Microsoft.Maui.Platform;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Window = Microsoft.UI.Xaml.Window;
using AppoMobi.Maui.Popups;
using AppoMobi.Maui.Popups;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Page = Microsoft.Maui.Controls.Page;
using Popup = AppoMobi.Maui.Popups.Popup;

namespace AppoMobi.Maui.Popups;

/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static partial class PopupExtensions
{

    static void PlatformShowPopup(Popup popup, IMauiContext mauiContext)
    {
        var window = mauiContext.GetPlatformWindow().GetWindow();

        if (window?.Content is not Page parent)
        {
            throw new InvalidOperationException("Window Content cannot be null");
        }

        if (window.Content is Element element)
        {
            element.AddLogicalChild(popup);
            var platform = popup.ToHandler(mauiContext);

            if (platform.PlatformView is MauiPopup native)
            {
                var root = element.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.ContentControl;
                if (root != null)
                {
                    if (root.Content is Microsoft.UI.Xaml.UIElement uiElement)
                    {
                        native.XamlRoot = uiElement.XamlRoot;
                    }
                }
            }

            platform?.Invoke(nameof(IPopup.OnOpened));
        }
    }

    static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext, CancellationToken token)
    {
        PlatformShowPopup(popup, mauiContext);
        return popup.Result.WaitAsync(token);
    }

    /// <summary>
    /// Method to update the <see cref="IPopup.OverlayColor"/>
    /// </summary>
    /// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
    /// <param name="popup">An instance of <see cref="IPopup"/>.</param>
    public static void SetBackgroundColor(this MauiPopup mauiPopup, IPopup popup)
	{
		if (mauiPopup.Overlay != null)
		{
			mauiPopup.Overlay.Background = new SolidColorBrush(popup.OverlayColor.ToWindowsColor());
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.Color"/> based on the <see cref="IPopup.Color"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetColor(this MauiPopup mauiPopup, IPopup popup)
	{


		var color = popup.Color ?? Colors.Transparent;
		if (mauiPopup.PopupView.Child is Panel panel)
		{
			panel.Background = color.ToPlatform();
		}
	}

	/// <summary>
	/// Method to update the popup anchor based on the <see cref="IPopup.Anchor"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void SetAnchor(this MauiPopup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		mauiPopup.PopupView.PlacementTarget = popup.Anchor?.ToPlatform(mauiContext);
	}

	/// <summary>
	/// Method to update the popup size based on the <see cref="IPopup.Size"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void SetSize(this MauiPopup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		ArgumentNullException.ThrowIfNull(popup.Content);

		const double defaultBorderThickness = 0;
		const double defaultSize = 600;

		var currentSize = new Size { Width = defaultSize, Height = defaultSize / 2 };

		var popupParent = mauiContext.GetPlatformWindow();
		var fullBounds = popupParent.Bounds;

		var popupParentFrame = fullBounds;
		if (!popup.IgnoreSafeArea)
		{
			popupParentFrame = GetSafeArea(mauiContext);
		}

		if (popup.Size.IsZero)
		{
			if (double.IsNaN(popup.Content.Width) || (double.IsNaN(popup.Content.Height)))
			{
				currentSize = popup.Content.Measure(double.IsNaN(popup.Content.Width) ? popupParentFrame.Width : popup.Content.Width,
					double.IsNaN(popup.Content.Height) ? popupParentFrame.Height : popup.Content.Height);

				if (double.IsNaN(popup.Content.Width))
				{
					currentSize.Width = popup.HorizontalOptions == LayoutAlignment.Fill ? popupParentFrame.Width : currentSize.Width;
				}
				if (double.IsNaN(popup.Content.Height))
				{
					currentSize.Height = popup.VerticalOptions == LayoutAlignment.Fill ? popupParentFrame.Height : currentSize.Height;
				}
			}
			else
			{
				currentSize.Width = popup.Content.Width;
				currentSize.Height = popup.Content.Height;
			}
		}
		else
		{
			currentSize.Width = popup.Size.Width;
			currentSize.Height = popup.Size.Height;
		}

		currentSize.Width = Math.Min(currentSize.Width, popupParentFrame.Width);
		currentSize.Height = Math.Min(currentSize.Height, popupParentFrame.Height);

		mauiPopup.PopupView.Width = currentSize.Width;
		mauiPopup.PopupView.Height = currentSize.Height;
		mauiPopup.PopupView.MinWidth = mauiPopup.PopupView.MaxWidth = currentSize.Width + (defaultBorderThickness * 2);
		mauiPopup.PopupView.MinHeight = mauiPopup.PopupView.MaxHeight = currentSize.Height + (defaultBorderThickness * 2);

		if (mauiPopup.PopupView.Child is FrameworkElement control)
		{
			control.Width = mauiPopup.PopupView.Width;
			control.Height = mauiPopup.PopupView.Height;
		}
	}

	/// <summary>
	///  Method to update the popup layout.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	public static void SetLayout(this MauiPopup mauiPopup, IPopup popup, IMauiContext? mauiContext)
	{
		ArgumentNullException.ThrowIfNull(mauiContext);
		ArgumentNullException.ThrowIfNull(popup.Content);

		var popupParent = mauiContext.GetPlatformWindow();
		var fullBounds = popupParent.Bounds;

		var popupParentFrame = fullBounds;
		if (!popup.IgnoreSafeArea)
		{
			popupParentFrame = GetSafeArea(mauiContext);
		}

		var contentSize = popup.Content.ToPlatform(mauiContext).DesiredSize;

		var isFlowDirectionRightToLeft = popup.Content?.FlowDirection == Microsoft.Maui.FlowDirection.RightToLeft;
		var horizontalOptionsPositiveNegativeMultiplier = isFlowDirectionRightToLeft ? -1 : 1;

		var verticalOptions = popup.VerticalOptions;
		var horizontalOptions = popup.HorizontalOptions;

		var center = 0;
		var verticalCenter = popupParentFrame.Top / 2;

		var horizontalEnd = (popupParentFrame.Width - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier) / 2;
		var horizontalStart = -horizontalEnd;

		var verticalStart = popupParentFrame.Top / 2 - (popupParentFrame.Height - contentSize.Height) / 2;
		var verticalEnd = (popupParentFrame.Height - contentSize.Height + popupParentFrame.Top) / 2;

		if (popup.Anchor is not null)
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Top;
		}
		else if (IsTopLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedLeft;
			mauiPopup.PopupView.HorizontalOffset = horizontalStart; 
			mauiPopup.PopupView.VerticalOffset = verticalStart;
		}
		else if (IsTop(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Top;
			mauiPopup.PopupView.HorizontalOffset = center; 
			mauiPopup.PopupView.VerticalOffset = verticalStart;
		}
		else if (IsTopRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.TopEdgeAlignedRight;
			mauiPopup.PopupView.HorizontalOffset = horizontalEnd;//(popupParentFrame.Width + popupParentFrame.Width * horizontalOptionsPositiveNegativeMultiplier) / 2 - contentSize.Width * horizontalOptionsPositiveNegativeMultiplier;
			mauiPopup.PopupView.VerticalOffset = verticalStart;
		}
		else if (IsRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Right;
			mauiPopup.PopupView.HorizontalOffset = horizontalEnd;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsBottomRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedRight;
			mauiPopup.PopupView.HorizontalOffset = horizontalEnd;
			mauiPopup.PopupView.VerticalOffset = verticalEnd;
		}
		else if (IsBottom(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Bottom;
			mauiPopup.PopupView.HorizontalOffset = center;
			mauiPopup.PopupView.VerticalOffset = verticalEnd;
		}
		else if (IsBottomLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
			mauiPopup.PopupView.HorizontalOffset = verticalStart;
			mauiPopup.PopupView.VerticalOffset = verticalEnd;
		}
		else if (IsLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Left;
			mauiPopup.PopupView.HorizontalOffset = horizontalStart;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsCenter(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = center;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsFillLeft(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = horizontalStart;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsFillCenter(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = center;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsFillRight(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = horizontalEnd;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsTopFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = center;
			mauiPopup.PopupView.VerticalOffset = verticalStart;
		}
		else if (IsCenterFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = 0;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else if (IsBottomFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = 0;
			mauiPopup.PopupView.VerticalOffset = verticalEnd;
		}
		else if (IsFill(verticalOptions, horizontalOptions))
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = 0;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}
		else
		{
			mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
			mauiPopup.PopupView.HorizontalOffset = 0;
			mauiPopup.PopupView.VerticalOffset = verticalCenter;
		}

		static bool IsTopLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Start;
		static bool IsTop(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Center;
		static bool IsTopRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.End;
		static bool IsRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.End;
		static bool IsBottomRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.End;
		static bool IsBottom(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Center;
		static bool IsBottomLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Start;
		static bool IsLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Start;
		static bool IsCenter(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Center;
		static bool IsFillLeft(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.Start;
		static bool IsFillCenter(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.Center;
		static bool IsFillRight(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.End;
		static bool IsTopFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Start && horizontalOptions == LayoutAlignment.Fill;
		static bool IsCenterFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Center && horizontalOptions == LayoutAlignment.Fill;
		static bool IsBottomFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.End && horizontalOptions == LayoutAlignment.Fill;
		static bool IsFill(LayoutAlignment verticalOptions, LayoutAlignment horizontalOptions) => verticalOptions == LayoutAlignment.Fill && horizontalOptions == LayoutAlignment.Fill;
	}

	/// <summary>
	/// Returns offsets taken by stem UI
	/// </summary>
	/// <param name="mauiContext"></param>
	/// <returns></returns>
	static Windows.Foundation.Rect GetSafeArea(IMauiContext mauiContext)
	{
		var platformWindow = mauiContext.GetPlatformWindow();
		var topOffset = 0;
		if (platformWindow.AppWindow.TitleBar != null)
		{
			var scale = platformWindow.AppWindow.Size.Width / platformWindow.Bounds.Width;
			topOffset = (int)(platformWindow.AppWindow.TitleBar.Height / scale + 4);
		}
		return new Windows.Foundation.Rect(platformWindow.Bounds.X, topOffset, platformWindow.Bounds.Width, platformWindow.Bounds.Height - topOffset);
	}

    public static Window GetPlatformWindow(this IMauiContext mauiContext) =>
        mauiContext.Services.GetRequiredService<Window>();

}