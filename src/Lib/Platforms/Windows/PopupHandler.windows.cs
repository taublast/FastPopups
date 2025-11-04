using System.Diagnostics;
using FastPopups;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace FastPopups;

#if WINDOWS

public partial class PopupHandler : ViewHandler<IPopup, MauiPopup>
{

    /// <summary>
    /// Action that's triggered when the Popup is Dismissed.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    /// <param name="result">The result that should return from this Popup.</param>
    public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
    {
        try
        {
            var window = view.GetWindow();
            if (window.Overlays.FirstOrDefault() is IWindowOverlay popupOverlay)
            {
                window.RemoveOverlay(popupOverlay); // this is NEVER INVOKED!!!
            }

            // Remove from navigation stack if it's a Popup
            if (view is Popup popup)
            {
                PopupNavigationStack.Instance.Remove(popup);
            }

            view.HandlerCompleteTCS.TrySetResult();
            handler.DisconnectHandler(handler.PlatformView);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
        }
    }

    /// <summary>
    /// Action that's triggered when the Popup is Opened.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    /// <param name="result">We don't need to provide the result parameter here.</param>
    public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
    {
        ArgumentNullException.ThrowIfNull(view.Parent);
        ArgumentNullException.ThrowIfNull(handler.MauiContext);

        var parent = view.Parent.ToPlatform(handler.MauiContext);
        parent.IsHitTestVisible = false;
        handler.PlatformView.XamlRoot = view.GetWindow().Content?.Handler?.MauiContext?.GetPlatformWindow().Content.XamlRoot ?? throw new InvalidOperationException("Window Content cannot be null");
        handler.PlatformView.IsHitTestVisible = true;

        handler.PlatformView.Show();
    }


    /// <summary>
    /// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    /// <param name="result">The result that should return from this Popup.</param>
    public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
    {
        view.OnDismissedByTappingOutsideOfPopup();
        handler.DisconnectHandler(handler.PlatformView);
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapAnchor(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.SetAnchor(view, handler.MauiContext);
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapCloseWhenBackgroundIsClicked(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.CloseWhenBackgroundIsClicked = view.CloseWhenBackgroundIsClicked;
    }


    /// <summary>
    /// Action that's triggered when the Popup BackgroundColor property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapBackgroundColor(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.SetBackgroundColor(view);
    }

    /// <summary>
    /// Action that's triggered when the Popup DisplayMode property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapDisplayMode(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.DisplayMode = view.DisplayMode;
        handler.PlatformView.Layout();
        handler.PlatformView.UpdateContentPositioning();
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapSize(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.Layout();
        handler.PlatformView.UpdateContentPositioning();
    }

    /// <summary>
    /// Action that's triggered when animation properties change.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapAnimation(PopupHandler handler, IPopup view)
    {
        // Animation properties are read directly from VirtualView when needed
        // No need to update platform view here
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(MauiPopup platformView)
    {
        // CRITICAL: Always call SetElement(null) to ensure proper cleanup
        // even if Parent is null (which happens when RemoveOverlay is called first)
        platformView.SetElement(null);

        // Only restore parent hit testing if Parent is still available
        if (VirtualView.Parent is not null && VirtualView.Handler?.MauiContext is not null)
        {
            var parent = VirtualView.Parent.ToPlatform(VirtualView.Handler.MauiContext);
            parent.IsHitTestVisible = true;
        }
    }

    /// <inheritdoc/>
    protected override MauiPopup CreatePlatformView()
    {
        _ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");

        return new MauiPopup(MauiContext);
    }

    /// <inheritdoc/>
    protected override void ConnectHandler(MauiPopup platformView)
    {
        _ = platformView.SetElement(VirtualView);

        base.ConnectHandler(platformView);
    }

}

#endif

