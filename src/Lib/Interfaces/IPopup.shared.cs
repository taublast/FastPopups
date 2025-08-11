using IElement = Microsoft.Maui.IElement;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace FastPopups;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public interface IPopup : IView, IVisualTreeElement, IAsynchronousHandler
{
	/// <summary>
	/// Gets the View that Popup will be anchored.
	/// </summary>
	IView? Anchor { get; }

	/// <summary>
	/// Gets the Popup's Content.
	/// </summary>
	IView? Content { get; }

	/// <summary>
	/// Gets the horizontal aspect of this element's arrangement in a container.
	/// </summary>
	LayoutOptions HorizontalOptions { get; }

	/// <summary>
	/// Controls whether the popup should respect safe insets like status bar etc.
	/// </summary>
	bool IsFullScreen { get; }

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping anywhere where gestures are not processed.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	bool CloseWhenBackgroundIsClicked { get; }

    /// <summary>
    /// Override this to allow latter processing of CloseWhenBackgroundIsClicked, return false to block.
    /// </summary>
    /// <returns></returns>
    bool OnBackgroundClicked();

    /// <summary>
    /// Gets the vertical aspect of this element's arrangement in a container.
    /// </summary>
    LayoutOptions VerticalOptions { get; }

	/// <summary>
	/// Occurs when the Popup is closed.
	/// </summary>
	/// <param name="result">Return value from the Popup.</param>
	void OnClosed(object? result = null);

	/// <summary>
	/// Occurs when the Popup is opened.
	/// </summary>
	void OnOpened();

	/// <summary>
	/// Occurs when the Popup is dismissed by a user tapping outside the Popup.
	/// </summary>
	void OnDismissedByTappingOutsideOfPopup();
}

