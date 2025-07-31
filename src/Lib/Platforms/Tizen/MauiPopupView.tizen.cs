using Tizen.NUI;

namespace AppoMobi.Maui.Popups;

#if TIZEN
/// <summary>
/// A View wrapper that manages a MauiPopup.
/// This allows us to use ViewHandler while still using Popup for popup functionality.
/// </summary>
public class MauiPopupView : View
{
	MauiPopup? popup;

	/// <summary>
	/// Constructor for MauiPopupView.
	/// </summary>
	public MauiPopupView()
	{
		// Make this view invisible - it's just a container for the popup
		Visibility = Visibility.Hidden;
		Opacity = 0;
	}

	/// <summary>
	/// Gets the internal popup.
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
	public void SetElement(IPopup popupElement)
	{
		popup?.SetElement(popupElement);
	}

	/// <summary>
	/// Shows the internal popup.
	/// </summary>
	public void ShowPopup()
	{
		popup?.Show();
	}

	/// <summary>
	/// Dismisses the internal popup.
	/// </summary>
	public void DismissPopup()
	{
		popup?.Dismiss();
	}

	/// <summary>
	/// Sets fullscreen mode on the internal popup.
	/// </summary>
	/// <param name="fullscreen">Whether to use fullscreen mode.</param>
	public void SetFullScreen(bool fullscreen)
	{
		popup?.SetFullScreen(fullscreen);
	}

	/// <summary>
	/// Gets the virtual view from the internal popup.
	/// </summary>
	public IPopup? VirtualView => popup?.VirtualView;

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