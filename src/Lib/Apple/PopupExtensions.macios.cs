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
		
		if (anchorView. Superview == null)
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
	/// Method to update the size of the Popup based on its content and layout options.
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
			double width;
			if (hasExplicitWidth == true && visualElement != null)
			{
				width = visualElement.WidthRequest;
			}
			else if (!double.IsNaN(popup.Content.Width))
			{
				width = popup.Content.Width;
			}
			else
			{
				// Need to measure content for auto-sizing width
				if (popup.Content.Handler == null)
				{
					_ = popup.Content.ToPlatform(
					popup.Handler?.MauiContext ?? throw new InvalidOperationException($"{nameof(popup.Handler.MauiContext)} Cannot Be Null"));
				}

				// Measure with infinite width or full width if Fill is requested
				var measureWidth = IsLayoutFill(popup.HorizontalOptions) ? adjustedFrame.Width : double.PositiveInfinity;
				var measureHeight = hasExplicitHeight == true && visualElement != null ? visualElement.HeightRequest : double.PositiveInfinity;

				var contentSize = popup.Content.Measure(measureWidth, measureHeight);

				// Use full width if Fill is requested, otherwise use measured width constrained to available space
				width = IsLayoutFill(popup.HorizontalOptions) ? adjustedFrame.Width : Math.Min(contentSize.Width, adjustedFrame.Width);
			}

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

				// Measure with the determined width and check if height should fill
				var measureWidth = width;
				var measureHeight = IsLayoutFill(popup.VerticalOptions) ? adjustedFrame.Height : double.PositiveInfinity;

				var contentSize = popup.Content.Measure(measureWidth, measureHeight);

				// Use full height if Fill is requested, otherwise use measured height constrained to available space
				height = IsLayoutFill(popup.VerticalOptions) ? adjustedFrame.Height : Math.Min(contentSize.Height, adjustedFrame.Height);
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

		// Calculate the padded size for content arrangement
		var paddedWidth = Math.Max(0, adjustedFrame.Width - popup.Padding.Left - popup.Padding.Right);
		var paddedHeight = Math.Max(0, adjustedFrame.Height - popup.Padding.Top - popup.Padding.Bottom);
		popup.Content.Arrange(new Rect(0, 0, paddedWidth, paddedHeight));
		
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

			// Calculate the available content size (subtract padding from total popup size)
			var totalSize = new Size(contentSize.Width, contentSize.Height);
			var availableSize = new Size(adjustedFrame.Width, adjustedFrame.Height);
			var availableContentSize = PopupLayoutCalculator.ApplyPadding(popup, totalSize, availableSize);

			// Compute the horizontal position based on the HorizontalOptions within the adjusted frame.
			// The content should be positioned within the padded frame area.
			nfloat x;
			if (IsLayoutStart(popup.HorizontalOptions))
			{
				x = adjustedFrame.X + (nfloat)popup.Padding.Left;
			}
			else if (IsLayoutEnd(popup.HorizontalOptions))
			{
				x = adjustedFrame.X + adjustedFrame.Width - (nfloat)availableContentSize.Width - (nfloat)popup.Padding.Right;
			}
			else if (IsLayoutCenter(popup.HorizontalOptions))
			{
				// Center the content within the padded frame area
				var paddedFrameX = adjustedFrame.X + (nfloat)popup.Padding.Left;
				var paddedFrameWidth = adjustedFrame.Width - (nfloat)popup.Padding.Left - (nfloat)popup.Padding.Right;
				x = paddedFrameX + (paddedFrameWidth - (nfloat)availableContentSize.Width) / 2;
			}
			else if (IsLayoutFill(popup.HorizontalOptions))
			{
				// For Fill, position at adjusted frame + left padding
				x = adjustedFrame.X + (nfloat)popup.Padding.Left;
			}
			else
			{
				// Default to center
				var paddedFrameX = adjustedFrame.X + (nfloat)popup.Padding.Left;
				var paddedFrameWidth = adjustedFrame.Width - (nfloat)popup.Padding.Left - (nfloat)popup.Padding.Right;
				x = paddedFrameX + (paddedFrameWidth - (nfloat)availableContentSize.Width) / 2;
			}

			// Compute the vertical position based on the VerticalOptions within the adjusted frame.
			// The content should be positioned within the padded frame area.
			nfloat y;
			if (IsLayoutStart(popup.VerticalOptions))
			{
				y = adjustedFrame.Y + additionalVerticalOffset + (nfloat)popup.Padding.Top;
			}
			else if (IsLayoutEnd(popup.VerticalOptions))
			{
				y = adjustedFrame.Y + adjustedFrame.Height - (nfloat)availableContentSize.Height - additionalVerticalOffset - (nfloat)popup.Padding.Bottom;
			}
			else if (IsLayoutCenter(popup.VerticalOptions))
			{
				// Center the content within the padded frame area
				var paddedFrameY = adjustedFrame.Y + (nfloat)popup.Padding.Top;
				var paddedFrameHeight = adjustedFrame.Height - (nfloat)popup.Padding.Top - (nfloat)popup.Padding.Bottom;
				y = paddedFrameY + (paddedFrameHeight - (nfloat)availableContentSize.Height) / 2 + additionalVerticalOffset;
			}
			else if (IsLayoutFill(popup.VerticalOptions))
			{
				// For Fill, position at adjusted frame + top padding
				y = adjustedFrame.Y + additionalVerticalOffset + (nfloat)popup.Padding.Top;
			}
			else
			{
				// Default to center
				var paddedFrameY = adjustedFrame.Y + (nfloat)popup.Padding.Top;
				var paddedFrameHeight = adjustedFrame.Height - (nfloat)popup.Padding.Top - (nfloat)popup.Padding.Bottom;
				y = paddedFrameY + (paddedFrameHeight - (nfloat)availableContentSize.Height) / 2 + additionalVerticalOffset;
			}

			if (mauiPopup.Control?.ViewController?.View is UIView contentView)
			{
				contentView.Frame = new CGRect(x, y, (nfloat)availableContentSize.Width, (nfloat)availableContentSize.Height);
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