using AppoMobi.Maui.FastPopups;
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
	/// <param name="contentSize">The size of the popup content (before padding is applied).</param>
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
		var adjustedBounds = popup.DisplayMode != PopupDisplayMode.Default 
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
			LayoutAlignment.Start => popup.DisplayMode != PopupDisplayMode.Default ? parentBounds.X : adjustedBounds.X,
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
			LayoutAlignment.Start => popup.DisplayMode != PopupDisplayMode.Default ? parentBounds.Y : adjustedBounds.Y,
			LayoutAlignment.End => adjustedBounds.Bottom - contentSize.Height,
			LayoutAlignment.Center or LayoutAlignment.Fill =>
				adjustedBounds.Y + (adjustedBounds.Height - contentSize.Height) / 2,
			_ => adjustedBounds.Y
		};

		return (x, y);
	}

	/// <summary>
	/// Calculates the available content size with padding applied.
	/// </summary>
	/// <param name="popup">The popup to calculate padding for.</param>
	/// <param name="totalSize">The total size before padding is applied.</param>
	/// <param name="availableSize">The available space to constrain against.</param>
	/// <returns>The content size with padding subtracted only when necessary.</returns>
	public static Size ApplyPadding(IPopup popup, Size totalSize, Size availableSize)
	{
		var padding = popup.Padding;
		var paddingWidth = padding.Left + padding.Right;
		var paddingHeight = padding.Top + padding.Bottom;

		// Only subtract padding if totalSize + padding would exceed available space
		var paddedWidth = totalSize.Width + paddingWidth > availableSize.Width
			? Math.Max(0, totalSize.Width - paddingWidth)
			: totalSize.Width;

		var paddedHeight = totalSize.Height + paddingHeight > availableSize.Height
			? Math.Max(0, totalSize.Height - paddingHeight)
			: totalSize.Height;

		return new Size(paddedWidth, paddedHeight);
	}

	/// <summary>
	/// Calculates the available content size with padding applied.
	/// </summary>
	/// <param name="popup">The popup to calculate padding for.</param>
	/// <param name="totalSize">The total size before padding is applied.</param>
	/// <returns>The content size with padding subtracted.</returns>
	public static Size ApplyPadding(IPopup popup, Size totalSize)
	{
		var padding = popup.Padding;
		var paddedWidth = Math.Max(0, totalSize.Width - padding.Left - padding.Right);
		var paddedHeight = Math.Max(0, totalSize.Height - padding.Top - padding.Bottom);
		return new Size(paddedWidth, paddedHeight);
	}

	/// <summary>
	/// Calculates the total size needed including padding.
	/// </summary>
	/// <param name="popup">The popup to calculate padding for.</param>
	/// <param name="contentSize">The desired content size.</param>
	/// <returns>The total size including padding.</returns>
	public static Size AddPadding(IPopup popup, Size contentSize)
	{
		var padding = popup.Padding;
		var totalWidth = contentSize.Width + padding.Left + padding.Right;
		var totalHeight = contentSize.Height + padding.Top + padding.Bottom;
		return new Size(totalWidth, totalHeight);
	}

	/// <summary>
	/// Calculates the content position offset due to padding.
	/// </summary>
	/// <param name="popup">The popup to calculate padding offset for.</param>
	/// <returns>The x, y offset for content positioning.</returns>
	public static (double X, double Y) GetPaddingOffset(IPopup popup)
	{
		var padding = popup.Padding;
		return (padding.Left, padding.Top);
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
		var adjustedBounds = popup.DisplayMode != PopupDisplayMode.Default
			? parentBounds
			: new Rect(
				parentBounds.X + safeAreaInsets.Left,
				parentBounds.Y + safeAreaInsets.Top,
				parentBounds.Width - safeAreaInsets.Left - safeAreaInsets.Right,
				parentBounds.Height - safeAreaInsets.Top - safeAreaInsets.Bottom);

		// Reduce available space by padding
		var paddedBounds = new Size(
			Math.Max(0, adjustedBounds.Width - popup.Padding.Left - popup.Padding.Right),
			Math.Max(0, adjustedBounds.Height - popup.Padding.Top - popup.Padding.Bottom));

		// If popup has explicit HeightRequest/WidthRequest, use them (but constrain to available space including padding)
		// Cast to VisualElement to access HeightRequest/WidthRequest properties
		var visualElement = popup as VisualElement;
		var hasExplicitWidth = visualElement?.WidthRequest > 0;
		var hasExplicitHeight = visualElement?.HeightRequest > 0;

		if (hasExplicitWidth == true && hasExplicitHeight == true && visualElement != null)
		{
			var requestedSize = new Size(
				Math.Min(visualElement.WidthRequest, paddedBounds.Width),
				Math.Min(visualElement.HeightRequest, paddedBounds.Height));
			return AddPadding(popup, requestedSize);
		}

		// If content has explicit size, use it
		if (popup.Content != null)
		{
			var explicitWidth = !double.IsNaN(popup.Content.Width) ? popup.Content.Width : double.NaN;
			var explicitHeight = !double.IsNaN(popup.Content.Height) ? popup.Content.Height : double.NaN;

			if (!double.IsNaN(explicitWidth) && !double.IsNaN(explicitHeight))
			{
				var contentSize = new Size(
					Math.Min(explicitWidth, paddedBounds.Width),
					Math.Min(explicitHeight, paddedBounds.Height));
				return AddPadding(popup, contentSize);
			}
		}

		// Calculate size based on layout options and any explicit requests
		var horizontalAlignment = GetLayoutAlignment(popup.HorizontalOptions);
		var verticalAlignment = GetLayoutAlignment(popup.VerticalOptions);

		var contentWidth = hasExplicitWidth == true && visualElement != null
			? Math.Min(visualElement.WidthRequest, paddedBounds.Width)
			: horizontalAlignment == LayoutAlignment.Fill
				? paddedBounds.Width
				: Math.Min(600, paddedBounds.Width); // Default width for autosize, constrained by available space

		var contentHeight = hasExplicitHeight == true && visualElement != null
			? Math.Min(visualElement.HeightRequest, paddedBounds.Height)
			: verticalAlignment == LayoutAlignment.Fill
				? paddedBounds.Height
				: Math.Min(400, paddedBounds.Height); // Default height for autosize, constrained by available space

		var finalContentSize = new Size(contentWidth, contentHeight);
		return AddPadding(popup, finalContentSize);
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