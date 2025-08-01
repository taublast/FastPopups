using AppoMobi.Maui.Popups;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace AppoMobi.Maui.Popups;

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
		var popupView = handler?.PlatformView;
		var popup = popupView?.Dialog;

		if (popup == null || popup.IsDisposed() || popup.Context.IsDisposed())
		{
			return;
		}

		if (!popup.Context.GetActivity().IsDestroyed)
		{
			if (popup.IsShowing)
			{
				popup.Dismiss();
			}
		}

		view.HandlerCompleteTCS.TrySetResult();

		if (popupView != null)
		{
			handler?.DisconnectHandler(popupView);
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
			handler.PlatformView.SetFullScreen(view.IgnoreSafeArea);
			handler.PlatformView.Dialog.SetSize(view, handler.Content, handler);
		}
	}

	/// <inheritdoc/>
	protected override MauiPopupView CreatePlatformView()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");
		_ = MauiContext.Context ?? throw new InvalidOperationException("Android Context is null, please check your MauiApplication.");

		var popupView = new MauiPopupView(MauiContext.Context);
		popupView.CreateDialog(MauiContext.Context, MauiContext);
		return popupView;
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiPopupView platformView)
	{
		Content = platformView.SetElement(VirtualView);

		if (Content is not null)
		{
			Content.LayoutChange += OnLayoutChange;
		}
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiPopupView platformView)
	{
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