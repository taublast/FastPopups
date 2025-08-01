using UIKit;
using Microsoft.Maui.Platform;

namespace AppoMobi.Maui.Popups;

#if IOS || MACCATALYST
/// <summary>
/// A UIView wrapper that manages a MauiPopup UIViewController.
/// This allows us to use ViewHandler while still using UIViewController for popup functionality.
/// </summary>
public class MauiPopupView : UIView
{
    MauiPopup? popup;

    /// <summary>
    /// Constructor for MauiPopupView.
    /// </summary>
    public MauiPopupView()
    {
        // Make this view invisible - it's just a container for the popup
        Hidden = true;
        Alpha = 0;
    }

    /// <summary>
    /// Gets the internal popup view controller.
    /// </summary>
    public MauiPopup? Popup => popup;

    /// <summary>
    /// Creates and configures the internal popup.
    /// </summary>
    /// <param name="mauiContext">MAUI Context.</param>
    public void CreatePopup(Microsoft.Maui.IMauiContext mauiContext)
    {
        popup?.Dispose();
        popup = new MauiPopup(mauiContext);
    }

    /// <summary>
    /// Sets the popup element on the internal popup.
    /// </summary>
    /// <param name="popupElement">The popup element.</param>
    /// <returns>The control handler.</returns>
    public Microsoft.Maui.Handlers.PageHandler? SetElement(IPopup popupElement)
    {
        popup?.SetElement(popupElement);
        return popup?.Control;
    }

    /// <summary>
    /// Shows the internal popup.
    /// </summary>
    public void ShowPopup()
    {
        // Create the control with a proper Page wrapper for the popup content
        if (popup?.VirtualView != null)
        {
            popup.CreateControl(virtualView => 
            {
                // Create a ContentPage to wrap the popup content
                var contentPage = new ContentPage
                {
                    Content = virtualView.Content as Microsoft.Maui.Controls.View
                };
                
                var mauiContext = virtualView.Handler?.MauiContext ?? throw new InvalidOperationException("MauiContext cannot be null");
                var pageHandler = contentPage.ToHandler(mauiContext) as Microsoft.Maui.Handlers.PageHandler;
                
                return pageHandler ?? throw new InvalidOperationException("Failed to create PageHandler for popup content");
            }, popup.VirtualView);
        }
    }

    /// <summary>
    /// Dismisses the internal popup.
    /// </summary>
    public void DismissPopup()
    {
        // iOS popup dismissal is handled via view controller dismissal
        // This will be handled by the popup extensions/handler
    }

    /// <summary>
    /// Sets fullscreen mode on the internal popup.
    /// </summary>
    /// <param name="fullscreen">Whether to use fullscreen mode.</param>
    public void SetFullScreen(bool fullscreen)
    {
        // iOS doesn't need explicit fullscreen handling like Android
        // The modal presentation handles this
    }

    /// <summary>
    /// Gets the virtual view from the internal popup.
    /// </summary>
    public IPopup? VirtualView => popup?.VirtualView;

    /// <summary>
    /// Gets the overlay from the internal popup.
    /// </summary>
    public UIView? PopupOverlay => popup?.Overlay;

    /// <summary>
    /// Gets the control from the internal popup.
    /// </summary>
    public Microsoft.Maui.Handlers.PageHandler? Control => popup?.Control;

    /// <summary>
    /// Gets the view controller from the internal popup.
    /// </summary>
    public UIViewController? ViewController => popup?.ViewController;

    /// <summary>
    /// Clean up resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            popup?.Dispose();
            popup = null;
        }

        base.Dispose(disposing);
    }
}

#endif