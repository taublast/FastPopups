#if MACCATALYST || IOS

using Microsoft.Maui.Platform;
using ObjCRuntime;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace FastPopups;
/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static partial class PopupExtensions
{
	/// <summary>
	/// Helper method to check if LayoutOptions represents Fill alignment.
	/// </summary>
	/// <param name="options">The LayoutOptions to check.</param>
	/// <returns>True if the options represent Fill alignment, false otherwise.</returns>
	static bool IsLayoutFill(LayoutOptions options)
	{
		return (int)options.Alignment == 3; // Fill = 3
	}

	/// <summary>
	/// Helper method to check if LayoutOptions represents Center alignment.
	/// </summary>
	/// <param name="options">The LayoutOptions to check.</param>
	/// <returns>True if the options represent Center alignment, false otherwise.</returns>
	static bool IsLayoutCenter(LayoutOptions options)
	{
		return (int)options.Alignment == 1; // Center = 1
	}

	/// <summary>
	/// Helper method to check if LayoutOptions represents Start alignment.
	/// </summary>
	/// <param name="options">The LayoutOptions to check.</param>
	/// <returns>True if the options represent Start alignment, false otherwise.</returns>
	static bool IsLayoutStart(LayoutOptions options)
	{
		return (int)options.Alignment == 0; // Start = 0
	}

	/// <summary>
	/// Helper method to check if LayoutOptions represents End alignment.
	/// </summary>
	/// <param name="options">The LayoutOptions to check.</param>
	/// <returns>True if the options represent End alignment, false otherwise.</returns>
	static bool IsLayoutEnd(LayoutOptions options)
	{
		return (int)options.Alignment == 2; // End = 2
	}

#if MACCATALYST
	// https://github.com/CommunityToolkit/Maui/pull/1361#issuecomment-1736487174
	static nfloat popupMargin = 18f;
#endif

	/// <summary>
	/// Helper method that returns an adjusted frame based on safe area insets and, on MacCatalyst, extra margins.
	/// </summary>
	/// <param name="mauiPopup">The popup instance.</param>
	/// <param name="ignoreSafeArea">If true, safe area insets are ignored.</param>
	/// <returns>A CGRect representing the available area.</returns>
	static CGRect GetAdjustedFrame(MauiPopup mauiPopup, bool ignoreSafeArea)
	{
		// Start with the full screen bounds.
		CGRect frame = UIScreen.MainScreen.Bounds;

		// If we're honoring safe areas, reduce the frame by the window's safe area insets.
		if (!ignoreSafeArea)
		{
			if (mauiPopup.View?.Window is not UIWindow window)
			{
				window = new UIWindow(frame: frame);
			}

			frame = new CGRect(
				frame.X + window.SafeAreaInsets.Left,
				frame.Y + window.SafeAreaInsets.Top,
				frame.Width - window.SafeAreaInsets.Left - window.SafeAreaInsets.Right,
				frame.Height - window.SafeAreaInsets.Top - window.SafeAreaInsets.Bottom);
		}
#if MACCATALYST
		// On MacCatalyst, further reduce the frame by the constant margin on all four sides.
		frame = new CGRect(
			frame.X + popupMargin,
			frame.Y + popupMargin,
			frame.Width - popupMargin * 2,
			frame.Height - popupMargin * 2);
#endif

		return frame;
	}

	/// <summary>
	/// Gets the bounds of an anchor view in screen coordinates.
	/// </summary>
	/// <param name="anchor">The anchor view.</param>
	/// <param name="mauiContext">The MAUI context.</param>
	/// <returns>The anchor bounds in screen coordinates.</returns>
	static Rect GetAnchorBounds(IView anchor, IMauiContext mauiContext)
	{
		var anchorView = anchor.ToPlatform(mauiContext);
		
		if (anchorView.Superview == null)
		{
			return Rect.Zero;
		}
		
		// Convert anchor frame to window coordinates (screen coordinates)
		var windowCoordinates = anchorView.Superview.ConvertRectToView(anchorView.Frame, null);
		
		return new Rect(
			windowCoordinates.X,
			windowCoordinates.Y,
			windowCoordinates.Width,
			windowCoordinates.Height);
	}


	/// <summary>
	/// Method to update the <see cref="IPopup.Size"/> of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetSize(this MauiPopup mauiPopup, in IPopup popup)
	{
		ArgumentNullException.ThrowIfNull(popup.Content);

		CGRect adjustedFrame = GetAdjustedFrame(mauiPopup, popup.IsFullScreen);

		CGSize currentSize;

		// Check if popup has explicit size requests
		// Cast to VisualElement to access HeightRequest/WidthRequest properties
		var visualElement = popup as VisualElement;
		var hasExplicitWidth = visualElement?.WidthRequest > 0;
		var hasExplicitHeight = visualElement?.HeightRequest > 0;
		
		if (hasExplicitWidth != true && hasExplicitHeight != true)
		{
			if (double.IsNaN(popup.Content.Width) || double.IsNaN(popup.Content.Height))
			{
				if (popup.Content.Handler == null)
				{
					_ = popup.Content.ToPlatform(
					popup.Handler?.MauiContext ?? throw new InvalidOperationException($"{nameof(popup.Handler.MauiContext)} Cannot Be Null"));
				}

				// For auto-sizing, measure with infinite space first to get natural content size
				// Only constrain by available space if explicitly requested via Fill layout options
				var measureWidth = double.IsNaN(popup.Content.Width)
					? (IsLayoutFill(popup.HorizontalOptions) ? adjustedFrame.Width : double.PositiveInfinity)
					: popup.Content.Width;

				var measureHeight = double.IsNaN(popup.Content.Height)
					? (IsLayoutFill(popup.VerticalOptions) ? adjustedFrame.Height : double.PositiveInfinity)
					: popup.Content.Height;

				var contentSize = popup.Content.Measure(measureWidth, measureHeight);

				var width = contentSize.Width;
				var height = contentSize.Height;

				if (double.IsNaN(popup.Content.Width))
				{
					width = IsLayoutFill(popup.HorizontalOptions) ? adjustedFrame.Width : width;
				}
				if (double.IsNaN(popup.Content.Height))
				{
					// For auto-sizing, only use full height if explicitly requested via Fill
					// Otherwise use the measured content height for proper auto-sizing
					height = IsLayoutFill(popup.VerticalOptions) ? adjustedFrame.Height : height;
				}

				// Constrain to available space to prevent popup from being larger than screen
				width = Math.Min(width, adjustedFrame.Width);
				height = Math.Min(height, adjustedFrame.Height);

				// IMPORTANT: If width was constrained, re-measure content with the final width
				// to ensure proper layout with the actual available space
				if (!IsLayoutFill(popup.HorizontalOptions) && width < contentSize.Width)
				{
					contentSize = popup.Content.Measure(width, double.PositiveInfinity);
					height = Math.Min(contentSize.Height, adjustedFrame.Height);
				}

				currentSize = new CGSize(width, height);
			}
			else
			{
				currentSize = new CGSize(popup.Content.Width, popup.Content.Height);
			}
		}
		else if (hasExplicitWidth == true && hasExplicitHeight == true && visualElement != null)
		{
			currentSize = new CGSize(visualElement.WidthRequest, visualElement.HeightRequest);
		}
		else
		{
			// Mixed case: some explicit, some calculated
			var width = hasExplicitWidth == true && visualElement != null ? visualElement.WidthRequest :
				double.IsNaN(popup.Content.Width) ? adjustedFrame.Width : popup.Content.Width;

			double height;
			if (hasExplicitHeight == true && visualElement != null)
			{
				height = visualElement.HeightRequest;
			}
			else if (!double.IsNaN(popup.Content.Height))
			{
				height = popup.Content.Height;
			}
			else
			{
				// Need to measure content for auto-sizing
				if (popup.Content.Handler == null)
				{
					_ = popup.Content.ToPlatform(
					popup.Handler?.MauiContext ?? throw new InvalidOperationException($"{nameof(popup.Handler.MauiContext)} Cannot Be Null"));
				}

				// Measure with the determined width and infinite height for auto-sizing
				var measureWidth = width;
				var measureHeight = double.PositiveInfinity; // Always use infinite for auto-sizing

				var contentSize = popup.Content.Measure(measureWidth, measureHeight);

				// Use measured height, but constrain to available space if needed
				height = Math.Min(contentSize.Height, adjustedFrame.Height);
			}

			currentSize = new CGSize(width, height);
		}

		currentSize.Width = NMath.Min(currentSize.Width, adjustedFrame.Width);
		currentSize.Height = NMath.Min(currentSize.Height, adjustedFrame.Height);

		mauiPopup.PreferredContentSize = currentSize;
	}

	/// <summary>
	/// Method to update the layout of the Popup and <see cref="IPopup.Content"/>.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetLayout(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (mauiPopup.View is null || popup.Content is null)
		{
			return;
		}

		// Get the original full screen bounds (for calculating additional offsets).
		CGRect originalFrame = UIScreen.MainScreen.Bounds;

		// Get the adjusted frame based on safe areas (and MacCatalyst margins).
		CGRect adjustedFrame = GetAdjustedFrame(mauiPopup, popup.IsFullScreen);

		popup.Content.Arrange(new Rect(0,0, adjustedFrame.Width, adjustedFrame.Height));
		
#if MACCATALYST
		var titleBarHeight = mauiPopup.ViewController?.NavigationController?.NavigationBar.Frame.Y ?? 0;
		var navigationBarHeight = mauiPopup.ViewController?.NavigationController?.NavigationBar.Frame.Size.Height ?? 0;
		bool isPortrait = originalFrame.Height >= originalFrame.Width;
		nfloat additionalVerticalOffset = isPortrait
			? (titleBarHeight + navigationBarHeight)
			: (titleBarHeight + navigationBarHeight) / 2;
#else
            nfloat additionalVerticalOffset = 0;
#endif

		if (popup.Anchor is null)
		{
			// Calculate the intrinsic content size.
			CGSize contentSize = mauiPopup.PreferredContentSize;
			if (!double.IsNaN(popup.Content.Width) && !IsLayoutFill(popup.HorizontalOptions))
			{
				contentSize.Width = (nfloat)popup.Content.Width;
			}
			if (!double.IsNaN(popup.Content.Height) && !IsLayoutFill(popup.VerticalOptions))
			{
				contentSize.Height = (nfloat)popup.Content.Height;
			}

			// Compute the horizontal position based on the HorizontalOptions within the adjusted frame.
			nfloat x;
			if (IsLayoutStart(popup.HorizontalOptions))
			{
				x = adjustedFrame.X;
			}
			else if (IsLayoutEnd(popup.HorizontalOptions))
			{
				x = adjustedFrame.X + adjustedFrame.Width - contentSize.Width;
			}
			else if (IsLayoutCenter(popup.HorizontalOptions) || IsLayoutFill(popup.HorizontalOptions))
			{
				x = adjustedFrame.X + (adjustedFrame.Width - contentSize.Width) / 2;
			}
			else
			{
				x = adjustedFrame.X + (adjustedFrame.Width - contentSize.Width) / 2;
			}

			// Compute the vertical position based on the VerticalOptions within the adjusted frame.
			nfloat y;
			if (IsLayoutStart(popup.VerticalOptions))
			{
				y = adjustedFrame.Y + additionalVerticalOffset;
			}
			else if (IsLayoutEnd(popup.VerticalOptions))
			{
				y = adjustedFrame.Y + adjustedFrame.Height - contentSize.Height - additionalVerticalOffset;
			}
			else if (IsLayoutCenter(popup.VerticalOptions) || IsLayoutFill(popup.VerticalOptions))
			{
				y = adjustedFrame.Y + (adjustedFrame.Height - contentSize.Height) / 2 - additionalVerticalOffset;
			}
			else
			{
				y = adjustedFrame.Y + (adjustedFrame.Height - contentSize.Height) / 2;
			}

			if (mauiPopup.Control?.ViewController?.View is UIView contentView)
			{
				contentView.Frame = new CGRect(x, y, contentSize.Width, contentSize.Height);
			}
		}
		else
		{
			// Use unified anchor positioning system
			var anchorBounds = GetAnchorBounds(popup.Anchor, popup.Handler?.MauiContext
											   ?? throw new InvalidOperationException($"{nameof(popup.Handler.MauiContext)} cannot be null"));
			
			// Get screen bounds and safe area info for PopupLayoutCalculator
			var screenBounds = new Rect(originalFrame.X, originalFrame.Y, originalFrame.Width, originalFrame.Height);
			var adjustedBounds = new Rect(adjustedFrame.X, adjustedFrame.Y, adjustedFrame.Width, adjustedFrame.Height);
			
			// Use PopupLayoutCalculator for consistent cross-platform positioning
			var calculatedPosition = PopupLayoutCalculator.CalculateAnchoredPosition(
				popup,
				new Size(mauiPopup.PreferredContentSize.Width, mauiPopup.PreferredContentSize.Height),
				anchorBounds,
				screenBounds);
			
			if (mauiPopup.Control?.ViewController?.View is UIView contentView)
			{
				contentView.Frame = new CGRect(
					calculatedPosition.X,
					calculatedPosition.Y + additionalVerticalOffset,
					mauiPopup.PreferredContentSize.Width,
					mauiPopup.PreferredContentSize.Height);
			}
		}
	}

	/// <summary>
	/// Method to update the BackgroundColor of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetBackgroundColor(this MauiPopup mauiPopup, in IPopup popup)
	{
		if (mauiPopup.Overlay==null)
		{
			return;
		}
		var color = ((Popup)popup).BackgroundColor ?? Colors.Transparent;
		mauiPopup.Overlay.BackgroundColor = color.ToPlatform();
	}


	/// <summary>
	/// Method to update the <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property of the Popup.
	/// </summary>
	/// <param name="mauiPopup">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetCloseWhenBackgroundIsClicked(this MauiPopup mauiPopup, in IPopup popup)
	{
		mauiPopup.CanBeDismissedByTappingInternal = popup.CloseWhenBackgroundIsClicked;
	}

}

#endif