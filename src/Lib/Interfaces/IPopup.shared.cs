using AppoMobi.Maui.FastPopups;
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
	/// Gets the padding around the popup content that reduces the available content area.
	/// </summary>
	/// <remarks>
	/// Padding creates an inner boundary within the popup, reducing the space available for content.
	/// This is different from margins, which would affect the popup's position relative to its parent.
	/// </remarks>
	Microsoft.Maui.Thickness Padding { get; }

	/// <summary>
	/// Gets the animation type for the popup content.
	/// Note: The background overlay always fades in/out synchronized with this animation's duration.
	/// </summary>
	PopupAnimationType AnimationType { get; }

	/// <summary>
	/// Gets the animation duration in milliseconds.
	/// This duration applies to both the content animation and the overlay fade.
	/// </summary>
	int AnimationDuration { get; }

	/// <summary>
	/// Gets the easing curve for the popup content animation.
	/// Note: The overlay fade always uses linear easing.
	/// </summary>
	PopupAnimationEasing AnimationEasing { get; }

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

	/// <summary>
	/// Asynchronously plays the popup's hide animation before closing.
	/// </summary>
	/// <param name="cancel">A cancellation token to observe while waiting for the animation to complete.</param>
	/// <returns>A task that represents the asynchronous animation operation.</returns>
	/// <remarks>
	/// <para>
	/// This method is called internally during the popup close lifecycle to play the configured
	/// hide animation (reverse of <see cref="AnimationType"/>) before the popup is removed from the screen.
	/// </para>
	/// <para>
	/// <b>Animation Timing:</b> The hide animation plays asynchronously. The popup waits for this animation
	/// to complete before finalizing the close operation and raising the Closed event.
	/// </para>
	/// <para>
	/// <b>Platform Behavior:</b>
	/// <list type="bullet">
	/// <item><description><b>Windows:</b> Uses XAML Storyboard animations with reverse keyframes</description></item>
	/// <item><description><b>Android:</b> Uses ObjectAnimator with reverse property values</description></item>
	/// <item><description><b>iOS/macOS:</b> Uses UIView.Animate with reverse transforms</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// If <see cref="AnimationType"/> is <see cref="PopupAnimationType.None"/>, this method completes immediately.
	/// </para>
	/// </remarks>
	Task AnimateOutAsync(CancellationToken cancel);
}

