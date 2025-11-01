using Microsoft.Maui.Platform;

namespace FastPopups;

#if !WINDOWS && !IOS && !MACCATALYST && !ANDROID && !TIZEN

public partial class PopupHandler : Microsoft.Maui.Handlers.ElementHandler<IPopup, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformElement() => throw new NotSupportedException();

	/// <summary>
	/// Action that's triggered when the Popup is closed.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnClosed(PopupHandler handler, IPopup view, object? result)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Action that's triggered when the Popup is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapOnOpened(PopupHandler handler, IPopup view, object? result)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Action that's triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	/// <param name="result">The result that should return from this Popup.</param>
	public static void MapOnDismissedByTappingOutsideOfPopup(PopupHandler handler, IPopup view, object? result)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Anchor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnchor(PopupHandler handler, IPopup view)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapCloseWhenBackgroundIsClicked(PopupHandler handler, IPopup view)
	{
		throw new NotSupportedException();
	}


	/// <summary>
	/// Action that's triggered when the Popup BackgroundColor property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapBackgroundColor(PopupHandler handler, IPopup view)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Action that's triggered when the Popup <see cref="IPopup.Size"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapSize(PopupHandler handler, IPopup view)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Action that's triggered when animation properties change.
	/// </summary>
	/// <param name="handler">An instance of <see cref="PopupHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IPopup"/>.</param>
	public static void MapAnimation(PopupHandler handler, IPopup view)
	{
		throw new NotSupportedException();
	}
}

#endif