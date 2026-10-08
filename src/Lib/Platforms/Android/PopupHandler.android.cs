using FastPopups;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace FastPopups;

#if ANDROID

public partial class PopupHandler : ViewHandler<IPopup, MauiPopupView>
{
    internal AView? Content { get; set; }
    internal int LastPopupWidth { get; set; }
    internal int LastPopupHeight { get; set; }
    internal double LastWindowWidth { get; set; }
    internal double LastWindowHeight { get; set; }

    /// <summary>
    /// Action that's triggered when the Popup is closed
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    /// <param name="result">The result that should return from this Popup.</param>
    public static void MapOnClosed(PopupHandler? handler, IPopup view, object? result)
    {
        // Untyped on purpose: the typed PlatformView getter throws "PlatformView cannot be null here" when the
        // handler is already disconnected, which happens when the page that showed the popup is torn down
        // while the close animation runs (DisconnectHandler then dismissed the dialog itself).
        var popupView = (handler as IElementHandler)?.PlatformView as MauiPopupView;
        try
        {
            var popup = popupView?.Dialog;
            if (popup != null && !popup.IsDisposed() && !popup.Context.IsDisposed()
                && popup.IsShowing && popup.Context.GetActivity() is { IsDestroyed: false })
            {
                popup.Dismiss();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            // Same contract as Apple: a closed popup always leaves the navigation stack and CloseAsync always
            // completes, otherwise a dead popup stays on top of the stack for CloseTop/Clear and the next show.
            if (view is Popup popupInstance)
            {
                try { PopupNavigationStack.Instance.Remove(popupInstance); } catch { }
            }

            view.HandlerCompleteTCS.TrySetResult();

            if (popupView != null)
            {
                try { ((IElementHandler?)handler)?.DisconnectHandler(); } catch { }
            }
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
        handler.PlatformView.ShowDialog();
    }

    /// <summary>
    /// Action that's triggered when the Popup is dismissed by tapping outside of the popup.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    /// <param name="result">The result that should return from this Popup.</param>
    public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
    {
        if (view.CloseWhenBackgroundIsClicked)
        {
            view.OnDismissedByTappingOutsideOfPopup();
        }
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapAnchor(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.Dialog?.SetAnchor(view);
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapCloseWhenBackgroundIsClicked(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.Dialog?.SetCloseWhenBackgroundIsClicked(view);
    }


    /// <summary>
    /// Action that's triggered when the Popup background color changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapBackgroundColor(PopupHandler handler, IPopup view)
    {
        handler.PlatformView.Dialog?.SetBackgroundColor(view);
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapSize(PopupHandler handler, IPopup view)
    {
        if (handler.Content != null && handler.PlatformView.Dialog != null)
        {
            handler.PlatformView.SetDisplayMode(view.DisplayMode);
            handler.PlatformView.Dialog.SetSize(view, handler.Content, handler);
        }
    }

    /// <summary>
    /// Action that's triggered when the Popup <see cref="IPopup.DisplayMode"/> property changes.
    /// Recreates the dialog with the new display mode setting.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapDisplayMode(PopupHandler handler, IPopup view)
    {
        var wasShowing = handler.PlatformView.Dialog?.IsShowing ?? false;

        // Clean up and recreate dialog
        RecreateDialog(handler, view);

        // Show dialog again if it was previously showing
        if (wasShowing)
        {
            handler.PlatformView.ShowDialog();
        }
    }

    /// <summary>
    /// Action that's triggered when animation properties change.
    /// </summary>
    /// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
    /// <param name="view">An instance of <see cref="IPopup"/>.</param>
    public static void MapAnimation(PopupHandler handler, IPopup view)
    {
        // Animation properties are read directly from VirtualView when needed
        // TODO: Implement Android animations in future
    }

    /// <summary>
    /// Creates or recreates the dialog with proper setup.
    /// </summary>
    /// <param name="handler">The popup handler.</param>
    /// <param name="view">The popup view.</param>
    static void RecreateDialog(PopupHandler handler, IPopup view)
    {
        var popupView = handler.PlatformView;

        // Clean up existing content and dialog
        CleanupExistingDialog(handler);

        // Create new dialog
        popupView.CreateDialog(handler.MauiContext.Context, handler.MauiContext, view.DisplayMode);

        // Set up content and handlers
        SetupDialogContent(handler, view);
    }

    /// <summary>
    /// Cleans up existing dialog and content.
    /// </summary>
    /// <param name="handler">The popup handler.</param>
    static void CleanupExistingDialog(PopupHandler handler)
    {
        // Detach layout change handler from old content
        if (handler.Content is not null)
        {
            handler.Content.LayoutChange -= handler.OnLayoutChange;
        }

        // Remove content from its current parent to avoid "child already has a parent" error
        if (handler.Content?.Parent is Android.Views.ViewGroup parent)
        {
            parent.RemoveView(handler.Content);
        }

        // Dismiss the old dialog; do NOT dispose here because Android may still
        // dispatch queued touch events to the native Dialog after Dismiss() returns,
        // and disposing the managed peer immediately causes TypeManager.CreateInstance
        // to crash. The dialog will be disposed when CreateDialog replaces it.
        if (handler.PlatformView.Dialog != null)
        {
            if (handler.PlatformView.Dialog.IsShowing)
            {
                handler.PlatformView.Dialog.Dismiss();
            }
        }
    }

    /// <summary>
    /// Sets up dialog content and applies all necessary configurations.
    /// </summary>
    /// <param name="handler">The popup handler.</param>
    /// <param name="view">The popup view.</param>
    static void SetupDialogContent(PopupHandler handler, IPopup view)
    {
        // Set the element to create content
        handler.Content = handler.PlatformView.SetElement(view);

        // Apply display mode and sizing
        if (handler.Content is not null && handler.PlatformView.Dialog is not null)
        {
            handler.PlatformView.SetDisplayMode(view.DisplayMode);
            handler.PlatformView.Dialog.SetSize(view, handler.Content, handler);

            // Attach layout change handler
            handler.Content.LayoutChange += handler.OnLayoutChange;
        }
    }

    /// <inheritdoc/>
    protected override MauiPopupView CreatePlatformView()
    {
        _ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");
        _ = MauiContext.Context ?? throw new InvalidOperationException("Android Context is null, please check your MauiApplication.");

        var popupView = new MauiPopupView(MauiContext.Context);
        return popupView;
    }

    /// <inheritdoc/>
    protected override void ConnectHandler(MauiPopupView platformView)
    {
        // Use the same logic as recreation to ensure consistency
        RecreateDialog(this, VirtualView);
    }

    /// <inheritdoc/>
    protected override void DisconnectHandler(MauiPopupView platformView)
    {
        // The Dialog is a window of its own and outlives the page that showed it. When the handler goes away
        // while the dialog is still showing (its page was replaced or disposed, e.g. during the close animation),
        // disposing it without Dismiss leaves the window and its overlay on top of the app, taking every touch.
        // iOS drops a presented popup together with its page, dismiss here to match.
        var dialog = platformView.Dialog;
        if (dialog != null && !dialog.IsDisposed() && !dialog.Context.IsDisposed()
            && dialog.IsShowing && dialog.Context.GetActivity() is { IsDestroyed: false })
        {
            dialog.Dismiss();
        }

        // a CloseAsync waiting for the dismissal would never complete: MapOnClosed cannot run without a platform view
        VirtualView?.HandlerCompleteTCS.TrySetResult();

        if (VirtualView?.Content?.Handler is IElementHandler contentHandler)
            contentHandler.DisconnectHandler();

        platformView.Dispose();

        if (Content is not null)
        {
            Content.LayoutChange -= OnLayoutChange;
        }
    }

    void OnShowed(object? sender, EventArgs args)
    {
        _ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");

        VirtualView.OnOpened();
    }

    void OnLayoutChange(object? sender, EventArgs e)
    {
        if (VirtualView?.Handler?.PlatformView is MauiPopupView popupView &&
            popupView.Dialog is Dialog dialog && Content is not null)
        {
            PopupExtensions.SetSize(dialog, VirtualView, Content, this);
        }
    }
}

#endif