using FastPopups;
using Microsoft.Maui.Handlers;

namespace FastPopups;

#if IOS || MACCATALYST

public partial class PopupHandler : ViewHandler<IPopup, MauiPopupView>
{
	/// <summary>
	/// Static constructor to modify the command mapper for iOS
	/// </summary>
	static PopupHandler()
	{
		// Add the iOS-specific command mappings
		PopUpCommandMapper[nameof(IPopup.OnOpened)] = MapOnOpened;
		PopUpCommandMapper[nameof(IPopup.OnClosed)] = MapOnClosed;
	}
	/// <summary>
	/// Action that's triggered when the Popup is Dismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static async void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
        try
        {
            var popup = handler.PlatformView.Popup;

            // CRITICAL: Run the hide animation BEFORE dismissing the view controller
            // This ensures CloseWithAnimationAsync has already been called by AnimateOutAsync,
            // so we don't dismiss with fade animation

            if (popup?.PresentationController?.PresentedViewController is UIViewController presentationViewController)
            {
                // Dismiss WITHOUT animation (animated: false) because our custom animation already ran
                await presentationViewController.DismissViewControllerAsync(false);
            }

            // Remove from navigation stack if it's a Popup
            if (view is Popup popupInstance)
            {
                PopupNavigationStack.Instance.Remove(popupInstance);
            }

            view.HandlerCompleteTCS.TrySetResult();

            handler.DisconnectHandler(handler.PlatformView);
        }
        catch (Exception e)
        {
            Console.WriteLine(e); //avoid MAUI crashing us with "PlatformView cannot be null here" in rare scenarios
        }
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
	{
		var popup = handler.PlatformView.Popup;
		if (popup != null && popup.IsViewLoaded && view.CloseWhenBackgroundIsClicked)
		{
			view.OnDismissedByTappingOutsideOfPopup();
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
		handler.PlatformView.ShowPopup();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupHandler handler, IPopup view)
	{
		var popup = handler.PlatformView.Popup;
		popup?.SetSize(view);
		popup?.SetLayout(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCloseWhenBackgroundIsClicked(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.Popup?.SetCloseWhenBackgroundIsClicked(view);
	}

	/// <summary>
	/// Action that's triggered when the Popup BackgroundColor property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapBackgroundColor(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.Popup?.SetBackgroundColor(view);
	}

	/// <summary>
	/// Action that's triggered when animation properties change.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnimation(PopupHandler handler, IPopup view)
	{
		// Animation properties are read directly from VirtualView when needed
		// TODO: Implement iOS/MacCatalyst animations in future
	}

	/// <summary>
	/// Action that's triggered when the Popup size-related properties change.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		var popup = handler.PlatformView.Popup;
		popup?.SetSize(view);
		popup?.SetLayout(view);
	}

	/// <inheritdoc/>
	protected override MauiPopupView CreatePlatformView()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");

		var popupView = new MauiPopupView();
		popupView.CreatePopup(MauiContext);
		return popupView;
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopupView platformView)
	{
		platformView.SetElement(VirtualView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopupView platformView)
	{
		platformView.Dispose();
	}
}

#endif