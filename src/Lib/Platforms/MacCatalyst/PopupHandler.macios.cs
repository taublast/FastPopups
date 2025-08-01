using AppoMobi.Maui.Popups;
using Microsoft.Maui.Handlers;

namespace AppoMobi.Maui.Popups;

#if IOS || MACCATALYST

public partial class PopupHandler : ViewHandler<IPopup, MauiPopupView>
{
	/// <summary>
	/// Action that's triggered when the Popup is Dismissed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static async void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		var popup = handler.PlatformView.Popup;
		var presentationController = popup?.PresentationController;
		if (presentationController?.PresentedViewController is UIViewController presentationViewController)
		{
			await presentationViewController.DismissViewControllerAsync(true);
		}

		view.HandlerCompleteTCS.TrySetResult();

		handler.DisconnectHandler(handler.PlatformView);
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
		if (popup == null)
		{
			throw new InvalidOperationException($"{nameof(handler.PlatformView.Popup)} cannot be null.");
		}

		if (popup.IsViewLoaded && view.CloseWhenBackgroundIsClicked)
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
		handler.PlatformView.Popup?.SetSize(view);
		handler.PlatformView.Popup?.SetLayout(view);
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
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.Popup?.SetSize(view);
		handler.PlatformView.Popup?.SetLayout(view);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopupView platformView)
	{
		base.ConnectHandler(platformView);
		platformView.SetElement(VirtualView);
	}

	/// <inheritdoc/>
	protected override MauiPopupView CreatePlatformView()
	{
		var popupView = new MauiPopupView();
		popupView.CreatePopup(MauiContext ?? throw new NullReferenceException(nameof(MauiContext)));
		return popupView;
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopupView platformView)
	{
		base.DisconnectHandler(platformView);
		platformView.Popup?.CleanUp();
	}
}

#endif