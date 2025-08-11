using FastPopups;
using Tizen.UIExtensions.NUI;

namespace FastPopups;

#if TIZEN

public partial class PopupHandler : Microsoft.Maui.Handlers.ViewHandler<IPopup, MauiPopupView>
{
	/// <summary>
	/// Action that's triggered when the Popup is closed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		var popup = handler.PlatformView.Popup;

		if (popup != null && popup.IsOpen)
		{
			popup.Close();
		}

		// Remove from navigation stack if it's a Popup
		if (view is Popup popupInstance)
		{
			PopupNavigationStack.Instance.Remove(popupInstance);
		}

		view.HandlerCompleteTCS.TrySetResult();

		handler.DisconnectHandler(handler.PlatformView);
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
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
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
		// On Tizen, Anchor only update when popup is opened
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCloseWhenBackgroundIsClicked(PopupHandler handler, IPopup view)
	{
		// this property directly access on platform view
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Color"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapColor(PopupHandler handler, IPopup view)
	{
		// this property directly access on platform view
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		handler.PlatformView.Popup?.UpdateContentSize();
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopupView platformView)
	{
		platformView.SetElement(VirtualView);
	}

	/// <inheritdoc/>
	protected override MauiPopupView CreatePlatformView()
	{
		var mauiContext = MauiContext ?? throw new InvalidOperationException("${nameof(MauiContext)} cannot be null");
		var popupView = new MauiPopupView();
		popupView.CreatePopup(mauiContext);
		return popupView;
	}
}

#endif