using Microsoft.Maui.Primitives;

namespace FastPopups;

/// <summary>
/// Cross-platform anchor positioning calculator that provides consistent anchor positioning logic across all platforms.
/// This eliminates platform-specific coordinate conversion issues by handling everything in a unified way.
/// </summary>
public static class CrossPlatformAnchorCalculator
{
    /// <summary>
    /// Calculates the final popup position based on anchor, using consistent cross-platform logic.
    /// </summary>
    /// <param name="popup">The popup to position.</param>
    /// <param name="contentSize">The size of the popup content in platform units.</param>
    /// <param name="anchorBounds">The bounds of the anchor element in platform units.</param>
    /// <param name="containerBounds">The bounds of the container/screen in platform units.</param>
    /// <param name="safeAreaInsets">Safe area insets to consider (optional).</param>
    /// <returns>The calculated position (x, y) for the popup.</returns>
    public static (double X, double Y) CalculateAnchoredPosition(
        IPopup popup,
        Size contentSize,
        Rect anchorBounds,
        Rect containerBounds,
        Microsoft.Maui.Thickness safeAreaInsets = default)
    {
        // Apply safe area adjustments if not ignored
        var adjustedBounds = popup.IsFullScreen 
            ? containerBounds
            : new Rect(
                containerBounds.X + safeAreaInsets.Left,
                containerBounds.Y + safeAreaInsets.Top,
                containerBounds.Width - safeAreaInsets.Left - safeAreaInsets.Right,
                containerBounds.Height - safeAreaInsets.Top - safeAreaInsets.Bottom);

        // Calculate horizontal position (centered on anchor)
        var x = anchorBounds.X + (anchorBounds.Width - contentSize.Width) / 2;

        // Ensure popup stays within horizontal bounds
        if (x < adjustedBounds.X)
            x = adjustedBounds.X;
        else if (x + contentSize.Width > adjustedBounds.Right)
            x = adjustedBounds.Right - contentSize.Width;

        // Calculate vertical position (prefer below anchor)
        var y = anchorBounds.Bottom;

        // Check if there's enough space below the anchor
        if (y + contentSize.Height > adjustedBounds.Bottom)
        {
            // Not enough space below, try above
            var aboveY = anchorBounds.Y - contentSize.Height;
            
            if (aboveY >= adjustedBounds.Y)
            {
                // Enough space above, use it
                y = aboveY;
            }
            else
            {
                // Not enough space above or below, center vertically in available space
                y = adjustedBounds.Y + (adjustedBounds.Height - contentSize.Height) / 2;
                
                // Ensure it doesn't go outside bounds
                y = Math.Max(adjustedBounds.Y, Math.Min(y, adjustedBounds.Bottom - contentSize.Height));
            }
        }

        // Ensure final position is within bounds
        x = Math.Max(adjustedBounds.X, Math.Min(x, adjustedBounds.Right - contentSize.Width));
        y = Math.Max(adjustedBounds.Y, Math.Min(y, adjustedBounds.Bottom - contentSize.Height));

        return (x, y);
    }

    /// <summary>
    /// Platform-specific anchor bounds retrieval interface.
    /// Each platform implements this to provide anchor bounds in their native coordinate system.
    /// </summary>
    public interface IAnchorBoundsProvider
    {
        /// <summary>
        /// Gets the bounds of the anchor element in platform-specific coordinates.
        /// The coordinates should be relative to the popup container (dialog/window).
        /// </summary>
        /// <param name="anchor">The anchor view.</param>
        /// <param name="mauiContext">The MAUI context.</param>
        /// <returns>The bounds of the anchor element in platform coordinates.</returns>
        Rect GetAnchorBounds(IView anchor, IMauiContext mauiContext);

        /// <summary>
        /// Gets the container bounds (screen/window bounds) in platform-specific coordinates.
        /// </summary>
        /// <param name="mauiContext">The MAUI context.</param>
        /// <param name="ignoreSafeArea">Whether to ignore safe areas.</param>
        /// <returns>The container bounds in platform coordinates.</returns>
        Rect GetContainerBounds(IMauiContext mauiContext, bool ignoreSafeArea);

        /// <summary>
        /// Gets the safe area insets in platform-specific coordinates.
        /// </summary>
        /// <param name="mauiContext">The MAUI context.</param>
        /// <returns>The safe area insets in platform coordinates.</returns>
        Microsoft.Maui.Thickness GetSafeAreaInsets(IMauiContext mauiContext);
    }
}