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

		// Remove from navigation stack if it's a Popup
		if (view is Popup popupInstance)
		{
			PopupNavigationStack.Instance.Remove(popupInstance);
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
			handler.PlatformView.SetFullScreen(view.IsFullScreen);
			handler.PlatformView.Dialog.SetSize(view, handler.Content, handler);
		}
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.IsFullScreen"/> property changes.
	/// Recreates the dialog with the new safe area setting.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapIsFullScreen(PopupHandler handler, IPopup view)
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
		popupView.CreateDialog(handler.MauiContext.Context, handler.MauiContext, view.IsFullScreen);

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

		// Properly dismiss and dispose the old dialog
		if (handler.PlatformView.Dialog != null)
		{
			if (handler.PlatformView.Dialog.IsShowing)
			{
				handler.PlatformView.Dialog.Dismiss(); // Remove from WindowManager first
			}
			handler.PlatformView.Dialog.Dispose(); // Then dispose managed resources
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

		// Apply fullscreen and sizing
		if (handler.Content is not null && handler.PlatformView.Dialog is not null)
		{
			handler.PlatformView.SetFullScreen(view.IsFullScreen);
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