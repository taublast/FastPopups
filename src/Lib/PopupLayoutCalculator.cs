using Microsoft.Maui.Primitives;
using MauiThickness = Microsoft.Maui.Thickness;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace FastPopups;

/// <summary>
/// Cross-platform popup layout calculator that provides consistent positioning logic across all platforms.
/// </summary>
public static class PopupLayoutCalculator
{
	/// <summary>
	/// Calculates the position of a popup based on its alignment options.
	/// </summary>
	/// <param name="popup">The popup to position.</param>
	/// <param name="contentSize">The size of the popup content.</param>
	/// <param name="parentBounds">The bounds of the parent container.</param>
	/// <param name="safeAreaInsets">Safe area insets to consider (optional).</param>
	/// <returns>The calculated position (x, y) for the popup.</returns>
	public static (double X, double Y) CalculatePosition(
		IPopup popup,
		Size contentSize,
		Rect parentBounds,
		MauiThickness safeAreaInsets = default)
	{
		// Apply safe area adjustments if not ignored
		var adjustedBounds = popup.IsFullScreen 
			? parentBounds
			: new Rect(
				parentBounds.X + safeAreaInsets.Left,
				parentBounds.Y + safeAreaInsets.Top,
				parentBounds.Width - safeAreaInsets.Left - safeAreaInsets.Right,
				parentBounds.Height - safeAreaInsets.Top - safeAreaInsets.Bottom);

		// Convert VisualElement LayoutOptions to positioning logic
		var horizontalAlignment = GetLayoutAlignment(popup.HorizontalOptions);
		var x = horizontalAlignment switch
		{
			// When IsFullScreen=false, Start should respect safe area insets
			// When IsFullScreen=true, Start should position at true edge
			LayoutAlignment.Start => popup.IsFullScreen ? parentBounds.X : adjustedBounds.X,
			LayoutAlignment.End => adjustedBounds.Right - contentSize.Width,
			LayoutAlignment.Center or LayoutAlignment.Fill =>
				adjustedBounds.X + (adjustedBounds.Width - contentSize.Width) / 2,
			_ => adjustedBounds.X
		};

		// Convert VisualElement LayoutOptions to positioning logic
		var verticalAlignment = GetLayoutAlignment(popup.VerticalOptions);
		var y = verticalAlignment switch
		{
			// When IsFullScreen=false, Start should respect safe area insets
			// When IsFullScreen=true, Start should position at true edge
			LayoutAlignment.Start => popup.IsFullScreen ? parentBounds.Y : adjustedBounds.Y,
			LayoutAlignment.End => adjustedBounds.Bottom - contentSize.Height,
			LayoutAlignment.Center or LayoutAlignment.Fill =>
				adjustedBounds.Y + (adjustedBounds.Height - contentSize.Height) / 2,
			_ => adjustedBounds.Y
		};

		return (x, y);
	}

	/// <summary>
	/// Calculates the size of a popup based on its size options and content.
	/// </summary>
	/// <param name="popup">The popup to size.</param>
	/// <param name="parentBounds">The bounds of the parent container.</param>
	/// <param name="safeAreaInsets">Safe area insets to consider (optional).</param>
	/// <returns>The calculated size for the popup content.</returns>
	public static Size CalculateContentSize(
		IPopup popup,
		Rect parentBounds,
		MauiThickness safeAreaInsets = default)
	{
		// Apply safe area adjustments if not ignored
		var adjustedBounds = popup.IsFullScreen 
			? parentBounds
			: new Rect(
				parentBounds.X + safeAreaInsets.Left,
				parentBounds.Y + safeAreaInsets.Top,
				parentBounds.Width - safeAreaInsets.Left - safeAreaInsets.Right,
				parentBounds.Height - safeAreaInsets.Top - safeAreaInsets.Bottom);

		// If popup has explicit HeightRequest/WidthRequest, use them (but constrain to parent bounds)
		// Cast to VisualElement to access HeightRequest/WidthRequest properties
		var visualElement = popup as VisualElement;
		var hasExplicitWidth = visualElement?.WidthRequest > 0;
		var hasExplicitHeight = visualElement?.HeightRequest > 0;
		
		if (hasExplicitWidth == true && hasExplicitHeight == true && visualElement != null)
		{
			return new Size(
				Math.Min(visualElement.WidthRequest, adjustedBounds.Width),
				Math.Min(visualElement.HeightRequest, adjustedBounds.Height));
		}

		// If content has explicit size, use it
		if (popup.Content != null)
		{
			var explicitWidth = !double.IsNaN(popup.Content.Width) ? popup.Content.Width : double.NaN;
			var explicitHeight = !double.IsNaN(popup.Content.Height) ? popup.Content.Height : double.NaN;

			if (!double.IsNaN(explicitWidth) && !double.IsNaN(explicitHeight))
			{
				return new Size(
					Math.Min(explicitWidth, adjustedBounds.Width),
					Math.Min(explicitHeight, adjustedBounds.Height));
			}
		}

		// Calculate size based on layout options and any explicit requests
		var horizontalAlignment = GetLayoutAlignment(popup.HorizontalOptions);
		var verticalAlignment = GetLayoutAlignment(popup.VerticalOptions);

		var width = hasExplicitWidth == true && visualElement != null
			? Math.Min(visualElement.WidthRequest, adjustedBounds.Width)
			: horizontalAlignment == LayoutAlignment.Fill
				? adjustedBounds.Width
				: 600; // Default width for autosize - don't constrain by parent to allow natural sizing

		var height = hasExplicitHeight == true && visualElement != null
			? Math.Min(visualElement.HeightRequest, adjustedBounds.Height)
			: verticalAlignment == LayoutAlignment.Fill
				? adjustedBounds.Height
				: 400; // Default height for autosize - don't constrain by parent to allow natural sizing

		return new Size(width, height);
	}

	/// <summary>
	/// Calculates both size and position for a popup in one call.
	/// </summary>
	/// <param name="popup">The popup to calculate layout for.</param>
	/// <param name="parentBounds">The bounds of the parent container.</param>
	/// <param name="safeAreaInsets">Safe area insets to consider (optional).</param>
	/// <returns>The calculated size and position for the popup.</returns>
	public static (Size Size, double X, double Y) CalculateLayout(
		IPopup popup,
		Rect parentBounds,
		MauiThickness safeAreaInsets = default)
	{
		var size = CalculateContentSize(popup, parentBounds, safeAreaInsets);
		var (x, y) = CalculatePosition(popup, size, parentBounds, safeAreaInsets);
		return (size, x, y);
	}

	/// <summary>
	/// Calculates anchor-based positioning for a popup.
	/// </summary>
	/// <param name="popup">The popup to position.</param>
	/// <param name="contentSize">The size of the popup content.</param>
	/// <param name="anchorBounds">The bounds of the anchor element.</param>
	/// <param name="parentBounds">The bounds of the parent container.</param>
	/// <returns>The calculated position (x, y) for the popup relative to the anchor.</returns>
	public static (double X, double Y) CalculateAnchoredPosition(
		IPopup popup,
		Size contentSize,
		Rect anchorBounds,
		Rect parentBounds)
	{
		// Default to showing popup below the anchor, centered horizontally
		var x = anchorBounds.X + (anchorBounds.Width - contentSize.Width) / 2;
		var y = anchorBounds.Bottom;

		// Ensure popup stays within parent bounds
		if (x < parentBounds.X)
			x = parentBounds.X;
		else if (x + contentSize.Width > parentBounds.Right)
			x = parentBounds.Right - contentSize.Width;

		if (y + contentSize.Height > parentBounds.Bottom)
		{
			// Show above anchor if there's not enough space below
			y = anchorBounds.Y - contentSize.Height;
			
			// If still doesn't fit, center vertically
			if (y < parentBounds.Y)
				y = parentBounds.Y + (parentBounds.Height - contentSize.Height) / 2;
		}

		return (Math.Max(parentBounds.X, x), Math.Max(parentBounds.Y, y));
	}

	/// <summary>
	/// Converts VisualElement LayoutOptions to LayoutAlignment for positioning calculations.
	/// </summary>
	/// <param name="layoutOptions">The LayoutOptions from VisualElement.</param>
	/// <returns>Corresponding LayoutAlignment.</returns>
	public static LayoutAlignment GetLayoutAlignment(LayoutOptions layoutOptions)
	{
		// Convert between different LayoutAlignment enums
		return (int)layoutOptions.Alignment switch
		{
			0 => LayoutAlignment.Start,
			1 => LayoutAlignment.Center,
			2 => LayoutAlignment.End,
			3 => LayoutAlignment.Fill,
			_ => LayoutAlignment.Center
		};
	}
}