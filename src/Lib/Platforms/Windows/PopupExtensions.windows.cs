using Microsoft.Maui.Platform;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Window = Microsoft.UI.Xaml.Window;
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
    /// Method to update the popup overlay background color using popup's BackgroundColor
    /// </summary>
    /// <param name="mauiPopup">An instance of <see cref="Popup"/>.</param>
    /// <param name="popup">An instance of <see cref="IPopup"/>.</param>
    public static void SetBackgroundColor(this MauiPopup mauiPopup, IPopup popup)
    {
        if (mauiPopup.Overlay != null)
        {
            // Handle transparency properly - avoid white blocking issues
            var backgroundColor = ((Popup)popup).BackgroundColor;
            if (backgroundColor != Colors.Transparent)
            {
                var windowsColor = backgroundColor.ToWindowsColor();
                mauiPopup.Overlay.Background = new SolidColorBrush(windowsColor);
            }
            else
            {
                mauiPopup.Overlay.Background = null; // Explicitly null for transparent overlays
            }
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
        
        // With fullscreen overlay architecture, we can't use native PlacementTarget
        // because it conflicts with fullscreen sizing. Instead, we'll handle anchor
        // positioning using our custom layout logic in SetLayout method.
        
        // Clear any existing placement target since we handle positioning manually
        mauiPopup.PopupView.PlacementTarget = null;
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


        if (double.IsNaN(popup.Content.Width) || (double.IsNaN(popup.Content.Height)))
        {
            currentSize = popup.Content.Measure(
                double.IsNaN(popup.Content.Width) ? popupParentFrame.Width : popup.Content.Width,
                double.IsNaN(popup.Content.Height) ? popupParentFrame.Height : popup.Content.Height);

            if (double.IsNaN(popup.Content.Width))
            {
                currentSize.Width = IsLayoutFill(popup.HorizontalOptions)
                    ? popupParentFrame.Width
                    : currentSize.Width;
            }

            if (double.IsNaN(popup.Content.Height))
            {
                currentSize.Height = IsLayoutFill(popup.VerticalOptions)
                    ? popupParentFrame.Height
                    : currentSize.Height;
            }
        }
        else
        {
            currentSize.Width = IsLayoutFill(popup.HorizontalOptions)
                ? popupParentFrame.Width
                : popup.Content.Width;
            currentSize.Height = IsLayoutFill(popup.VerticalOptions)
                ? popupParentFrame.Height
                : popup.Content.Height;
        }


        currentSize.Width = Math.Min(currentSize.Width, popupParentFrame.Width);
        currentSize.Height = Math.Min(currentSize.Height, popupParentFrame.Height);

        // IMPORTANT: Make WinUI Popup fullscreen so overlay can fill entire screen
        // Content positioning will be handled within the fullscreen popup
        var window = mauiContext.GetPlatformWindow();
        var windowBounds = window.Bounds;
        
        mauiPopup.PopupView.Width = windowBounds.Width;
        mauiPopup.PopupView.Height = windowBounds.Height;
        mauiPopup.PopupView.MinWidth = mauiPopup.PopupView.MaxWidth = windowBounds.Width;
        mauiPopup.PopupView.MinHeight = mauiPopup.PopupView.MaxHeight = windowBounds.Height;

        if (mauiPopup.PopupView.Child is FrameworkElement control)
        {
            control.Width = windowBounds.Width;
            control.Height = windowBounds.Height;
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

        // Convert Windows.Foundation.Rect to Microsoft.Maui.Graphics.Rect
        var parentBounds = new Rect(fullBounds.X, fullBounds.Y, fullBounds.Width, fullBounds.Height);
        
        // Get safe area if needed
        Microsoft.Maui.Thickness safeAreaInsets = default;
        if (!popup.IgnoreSafeArea)
        {
            var safeArea = GetSafeArea(mauiContext);
            safeAreaInsets = new Microsoft.Maui.Thickness(
                safeArea.X - fullBounds.X,
                safeArea.Y - fullBounds.Y,
                fullBounds.Right - safeArea.Right,
                fullBounds.Bottom - safeArea.Bottom);
        }

        // Get content size - ensure proper measurement
        var platformContent = popup.Content.ToPlatform(mauiContext);
        
        // If DesiredSize is empty, measure the content first
        if (platformContent.DesiredSize.Width == 0 || platformContent.DesiredSize.Height == 0)
        {
            platformContent.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        }
        
        var contentSize = platformContent.DesiredSize;
        var mauiContentSize = new Size(contentSize.Width, contentSize.Height);
        
        // If still zero size, fall back to reasonable defaults or skip layout
        if (mauiContentSize.Width == 0 || mauiContentSize.Height == 0)
        {
            mauiContentSize = new Size(300, 200); // Reasonable default
        }

        // Handle anchor positioning using our custom logic
        // (native PlacementTarget conflicts with fullscreen popup)
        // Content positioning will be handled in MauiPopup.PositionContentInContainer

        // WinUI Popup is now fullscreen, so no need to position it
        // Content positioning is handled within the fullscreen popup by the composite content
        mauiPopup.PopupView.DesiredPlacement = PopupPlacementMode.Auto;
        mauiPopup.PopupView.HorizontalOffset = 0;
        mauiPopup.PopupView.VerticalOffset = 0;

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

        return new Windows.Foundation.Rect(platformWindow.Bounds.X, topOffset, platformWindow.Bounds.Width,
            platformWindow.Bounds.Height - topOffset);
    }

    public static Window GetPlatformWindow(this IMauiContext mauiContext) =>
        mauiContext.Services.GetRequiredService<Window>();

    /// <summary>
    /// Helper method to check if LayoutOptions is Fill
    /// </summary>
    /// <param name="layoutOptions">The LayoutOptions to check</param>
    /// <returns>True if Fill, false otherwise</returns>
    static bool IsLayoutFill(LayoutOptions layoutOptions)
    {
        return (int)layoutOptions.Alignment == 3; // Fill = 3
    }

    /// <summary>
    /// Gets the bounds of the anchor view in screen coordinates for Windows.
    /// </summary>
    /// <param name="anchor">The anchor view.</param>
    /// <param name="mauiContext">The MAUI context.</param>
    /// <returns>The bounds of the anchor view.</returns>
    public static Rect GetAnchorBounds(IView anchor, IMauiContext mauiContext)
    {
        if (anchor.Handler?.PlatformView is not FrameworkElement anchorElement)
        {
            return new Rect(0, 0, 100, 50); // Default if anchor not found
        }

        // Get anchor position relative to the window
        var window = mauiContext.GetPlatformWindow();
        var windowBounds = window.Bounds;
        
        try
        {
            // Transform anchor bounds to screen coordinates
            var transform = anchorElement.TransformToVisual(window.Content as FrameworkElement);
            var anchorPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            
            return new Rect(anchorPosition.X, anchorPosition.Y, anchorElement.ActualWidth, anchorElement.ActualHeight);
        }
        catch
        {
            // Fallback if transform fails
            return new Rect(0, 0, anchorElement.ActualWidth, anchorElement.ActualHeight);
        }
    }
}