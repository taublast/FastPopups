using System;
using System.Diagnostics;
using AppoMobi.Maui.FastPopups;
using View = Microsoft.Maui.Controls.View;

namespace FastPopups;

/// <summary>
/// Represents a small View that pops up at front the Page. Implements <see cref="IPopup"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Animation Lifecycle:</b> Popup animations follow a precise 3-phase lifecycle to ensure smooth,
/// glitch-free animations across all platforms:
/// </para>
/// <list type="number">
/// <item>
/// <term>Phase 1: Prepare (Before Show)</term>
/// <description>
/// Animation initial state is set BEFORE the popup becomes visible. This ensures the popup appears
/// in its "start" position (e.g., off-screen for slide animations, scaled down for zoom animations).
/// <br/><b>Timing:</b> Happens during platform view creation, before native presentation.
/// </description>
/// </item>
/// <item>
/// <term>Phase 2: Wait for Size</term>
/// <description>
/// The system waits for the popup view to be laid out with proper bounds. This is CRITICAL because
/// animations need accurate dimensions (e.g., container height for slide-from-bottom).
/// <br/><b>Platform Detection:</b>
/// <list type="bullet">
/// <item><description><b>Windows:</b> Checks ActualHeight/ActualWidth in Loaded event</description></item>
/// <item><description><b>Android:</b> Uses OnLayoutChangeListener to detect Width > 0 &amp;&amp; Height > 0</description></item>
/// <item><description><b>iOS:</b> Checks Bounds.Width/Height in ViewDidLayoutSubviews</description></item>
/// </list>
/// </description>
/// </item>
/// <item>
/// <term>Phase 3: Animate (After Size Available)</term>
/// <description>
/// The show animation plays, transitioning from the initial state to the final visible state.
/// The overlay fade-in animates in parallel. <see cref="Opened"/> event fires after animation completes.
/// <br/><b>Duration:</b> Controlled by <see cref="AnimationDuration"/> (default 250ms).
/// <br/><b>Easing:</b> Controlled by <see cref="AnimationEasing"/> enum.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Hide Animation:</b> When closing, the process reverses:
/// <list type="number">
/// <item><description>Hide animation plays (symmetrical reverse of show animation)</description></item>
/// <item><description>Platform native dismissal occurs</description></item>
/// <item><description><see cref="Closed"/> event fires with result</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important:</b> Never assume views have size immediately. Always wait for the platform's
/// layout pass to complete before reading dimensions or starting animations.
/// </para>
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// var popup = new Popup
/// {
///     Content = new Label { Text = "Hello!" },
///     AnimationType = PopupAnimationType.FromBottom,
///     AnimationDuration = 300,
///     AnimationEasing = PopupAnimationEasing.Spring
/// };
///
/// popup.Opened += (s, e) => Debug.WriteLine("Animation complete, popup visible");
/// popup.Closed += (s, e) => Debug.WriteLine($"Popup closed with result: {e.Result}");
///
/// await popup.Show();
/// var result = await popup.Result; // Wait for user interaction
/// </code>
/// </example>
[ContentProperty(nameof(Content))]
public partial class Popup : View, IPopup
//, IWindowController, IPropertyPropagationController, IResourcesProvider, IStyleSelectable, IStyleElement
{
	/// <summary>
	///  Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View),
		typeof(Popup), propertyChanged: OnContentChanged);



	/// <summary>
	/// Backing BindableProperty for the <see cref="DisplayMode"/> property.
	/// </summary>
	public static readonly BindableProperty DisplayModeProperty =
		BindableProperty.Create(nameof(DisplayMode), typeof(PopupDisplayMode), typeof(Popup), PopupDisplayMode.Default);


	/// <summary>
	///  Backing BindableProperty for the <see cref="CloseWhenBackgroundIsClicked"/> property.
	/// </summary>
	public static readonly BindableProperty CloseWhenBackgroundIsClickedProperty =
		BindableProperty.Create(nameof(CloseWhenBackgroundIsClicked), typeof(bool), typeof(Popup), true);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Padding"/> property.
	/// </summary>
	public static readonly BindableProperty PaddingProperty =
		BindableProperty.Create(nameof(Padding), typeof(Microsoft.Maui.Thickness), typeof(Popup), new Microsoft.Maui.Thickness(0));

	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimationType"/> property.
	/// </summary>
	public static readonly BindableProperty AnimationTypeProperty =
		BindableProperty.Create(nameof(AnimationType), typeof(PopupAnimationType), typeof(Popup), PopupAnimationType.ZoomIn);

	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimationDuration"/> property.
	/// </summary>
	public static readonly BindableProperty AnimationDurationProperty =
		BindableProperty.Create(nameof(AnimationDuration), typeof(int), typeof(Popup), 250); // Default 250ms

	/// <summary>
	/// Backing BindableProperty for the <see cref="AnimationEasing"/> property.
	/// </summary>
	public static readonly BindableProperty AnimationEasingProperty =
		BindableProperty.Create(nameof(AnimationEasing), typeof(PopupAnimationEasing), typeof(Popup), PopupAnimationEasing.Default);


	readonly WeakEventManager dismissWeakEventManager = new();
	readonly WeakEventManager openedWeakEventManager = new();
 

	TaskCompletionSource popupDismissedTaskCompletionSource = new();
	TaskCompletionSource<object?> resultTaskCompletionSource = new();
	

	/// <summary>
	/// Instantiates a new instance of <see cref="Popup"/>.
	/// </summary>
	public Popup()
	{
		// Set default layout alignment to center (equivalent to old custom properties)
		HorizontalOptions = LayoutOptions.Center;
		VerticalOptions = LayoutOptions.Center;
		
		// Set default overlay color using BackgroundColor (replaces old OverlayColor default)
		BackgroundColor = Color.FromRgba(1, 0, 0, 153); // Same default as old OverlayColor
	}

	/// <summary>
	/// Dismissed event is invoked when the popup is closed.
	/// </summary>
	public event EventHandler<PopupClosedEventArgs> Closed
	{
		add => dismissWeakEventManager.AddEventHandler(value);
		remove => dismissWeakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Opened event is invoked when the popup is opened.
	/// </summary>
	public event EventHandler<PopupOpenedEventArgs> Opened
	{
		add => openedWeakEventManager.AddEventHandler(value);
		remove => openedWeakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets the final result of the dismissed popup.
	/// </summary>
	public Task<object?> Result => resultTaskCompletionSource.Task;

	/// <summary>
	/// Gets or sets the <see cref="View"/> content to render in the Popup.
	/// </summary>
	/// <remarks>
	/// The View can be or type: <see cref="View"/>, <see cref="ContentPage"/> or <see cref="NavigationPage"/>
	/// </remarks>
	public virtual View? Content
	{
		get => (View?)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}





	/// <summary>
	/// Controls how the popup should be displayed in relation to system UI (status bar, navigation bar, etc.).
	/// </summary>
	public PopupDisplayMode DisplayMode
	{
		get => (PopupDisplayMode)GetValue(DisplayModeProperty);
		set => SetValue(DisplayModeProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping anywhere where gestures are not processed.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CloseWhenBackgroundIsClicked
	{
		get => (bool)GetValue(CloseWhenBackgroundIsClickedProperty);
		set => SetValue(CloseWhenBackgroundIsClickedProperty, value);
	}

	/// <summary>
	/// Gets or sets the padding around the popup content that reduces the available content area.
	/// </summary>
	/// <remarks>
	/// Padding creates an inner boundary within the popup, reducing the space available for content.
	/// This is different from margins, which would affect the popup's position relative to its parent.
	/// </remarks>
	public Microsoft.Maui.Thickness Padding
	{
		get => (Microsoft.Maui.Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	/// <summary>
	/// Gets or sets the animation type for the popup content.
	/// </summary>
	/// <remarks>
	/// The background overlay always fades in/out synchronized with this animation's duration.
	/// </remarks>
	public PopupAnimationType AnimationType
	{
		get => (PopupAnimationType)GetValue(AnimationTypeProperty);
		set => SetValue(AnimationTypeProperty, value);
	}

	/// <summary>
	/// Gets or sets the animation duration in milliseconds.
	/// </summary>
	/// <remarks>
	/// This duration applies to both the content animation and the overlay fade.
	/// Default is 250ms.
	/// </remarks>
	public int AnimationDuration
	{
		get => (int)GetValue(AnimationDurationProperty);
		set => SetValue(AnimationDurationProperty, value);
	}

	/// <summary>
	/// Gets or sets the easing curve for the popup content animation.
	/// </summary>
	/// <remarks>
	/// <para>The overlay fade always uses linear easing regardless of this setting.</para>
	/// </remarks>
	public PopupAnimationEasing AnimationEasing
	{
		get => (PopupAnimationEasing)GetValue(AnimationEasingProperty);
		set => SetValue(AnimationEasingProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="View"/> anchor.
	/// </summary>
	/// <remarks>
	/// The Anchor is where the Popup will render closest to. When an Anchor is configured,
	/// the popup will appear centered over that control or as close as possible.
	/// </remarks>
	public View? Anchor { get; set; }

 
	/// <summary>
	/// Gets or sets the result that will return when the user taps outside the Popup.
	/// </summary>
	protected object? ResultWhenUserTapsOutsideOfPopup { get; set; }

	/// <inheritdoc/>
	IView? IPopup.Anchor => Anchor;

	/// <inheritdoc/>
	IView? IPopup.Content => Content;

	/// <inheritdoc/>
	TaskCompletionSource IAsynchronousHandler.HandlerCompleteTCS => popupDismissedTaskCompletionSource;

	/// <summary>
	/// Resets the Popup.
	/// </summary>
	public void Reset()
	{
		resultTaskCompletionSource = new();
		popupDismissedTaskCompletionSource = new();
	}

	/// <summary>
	/// Close the current popup.
	/// </summary>
	/// <remarks>
	/// <see cref="Close(object?)"/> is an <see langword="async"/> <see langword="void"/> method, commonly referred to as a fire-and-forget method.
	/// It will complete and return to the calling thread before the operating system has dismissed the <see cref="Popup"/> from the screen.
	/// If you need to pause the execution of your method until the operating system has dismissed the <see cref="Popup"/> from the screen, use instead <see cref="CloseAsync(object?, CancellationToken)"/>.
	/// </remarks>
	/// <param name="result">The result to return.</param>
	public async void Close(object? result = null) => await CloseAsync(result, CancellationToken.None);

	/// <summary>
	/// Close the current popup.
	/// </summary>
	/// <remarks>
	/// Returns once the operating system has dismissed the <see cref="IPopup"/> from the page
	/// </remarks>
	/// <param name="result">The result to return.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	public async Task CloseAsync(object? result = null, CancellationToken token = default)
	{
		await OnClosed(result, false, token);
		resultTaskCompletionSource.TrySetResult(result);
	}

    /// <summary>
    /// Override this to allow latter processing of CloseWhenBackgroundIsClicked, return false to block.
    /// </summary>
    /// <returns></returns>
    public virtual bool OnBackgroundClicked()
    {
        return true;
    }

    internal bool ShouldDismissOnOutsideClick()
    {
        return CloseWhenBackgroundIsClicked && OnBackgroundClicked();
    }

    /// <summary>
    /// Invokes the <see cref="Opened"/> event.
    /// </summary>
    internal virtual void OnOpened() =>
		openedWeakEventManager.HandleEvent(this, PopupOpenedEventArgs.Empty, nameof(Opened));

	/// <summary>
	/// Invokes the <see cref="Closed"/> event.
	/// </summary>
	/// <param name="result">Sets the <see cref="PopupClosedEventArgs"/> Property of <see cref="PopupClosedEventArgs.Result"/>.</param>
	/// <param name="wasDismissedByTappingOutsideOfPopup">Sets the <see cref="PopupClosedEventArgs"/> Property of <see cref="PopupClosedEventArgs.WasDismissedByTappingOutsideOfPopup"/>/>.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	protected virtual async Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup,
		CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

        await ((IPopup)this).AnimateOutAsync(token);

        ((IPopup)this).OnClosed(result);

		RemoveBinding(Popup.DisplayModeProperty);
		RemoveBinding(Popup.ContentProperty);
		RemoveBinding(Popup.CloseWhenBackgroundIsClickedProperty);
		RemoveBinding(Popup.PaddingProperty);
		RemoveBinding(Popup.AnimationTypeProperty);
		RemoveBinding(Popup.AnimationDurationProperty);
		RemoveBinding(Popup.AnimationEasingProperty);
		RemoveBinding(Popup.StyleProperty);

		await popupDismissedTaskCompletionSource.Task.WaitAsync(token);

		Parent?.RemoveLogicalChild(this);

		dismissWeakEventManager.HandleEvent(this, new PopupClosedEventArgs(result, wasDismissedByTappingOutsideOfPopup),
			nameof(Closed));
	}

	/// <summary>
	/// Invoked when the popup is dismissed by tapping outside the popup.
	/// </summary>
	protected internal virtual async Task OnDismissedByTappingOutsideOfPopup(CancellationToken token = default)
	{
		await OnClosed(ResultWhenUserTapsOutsideOfPopup, true, token);
		resultTaskCompletionSource.TrySetResult(ResultWhenUserTapsOutsideOfPopup);
	}

	/// <summary>
	///<inheritdoc/>
	/// </summary>
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Content is not null)
		{
			SetInheritedBindingContext(Content, BindingContext);

			if (ReferenceEquals(Content.Parent, this) is false)
			{
				Content.Parent = null;
				Content.Parent = this;
			}
            else
            {
                Content.BindingContext = this.BindingContext; //for some reason was needed for iOS
            }
		}
	}

    static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
	{
        var popup = (Popup)bindable;
        popup.OnBindingContextChanged();
	}


	void IPopup.OnClosed(object? result) => Handler?.Invoke(nameof(IPopup.OnClosed), result);

    Task IPopup.AnimateOutAsync(CancellationToken cancel)
    {
#if WINDOWS
        if (Handler is PopupHandler handler)
        {
            return handler.PlatformView.CloseWithAnimationAsync();
        }
#elif ANDROID
        if (Handler is PopupHandler handler)
        {
            var dialog = handler.PlatformView.Dialog;
            if (dialog != null)
            {
                return dialog.CloseWithAnimationAsync();
            }
        }
#elif MACCATALYST || IOS
        if (Handler is PopupHandler handler && handler.PlatformView.Popup != null)
        {
            return handler.PlatformView.Popup.CloseWithAnimationAsync();
        }
#endif

        return Task.CompletedTask;
    }

    void IPopup.OnOpened() => OnOpened();

	async void IPopup.OnDismissedByTappingOutsideOfPopup() =>
		await OnDismissedByTappingOutsideOfPopup(CancellationToken.None);

	// Explicit interface implementations to resolve conflict with View properties
	LayoutOptions IPopup.HorizontalOptions => HorizontalOptions;
	LayoutOptions IPopup.VerticalOptions => VerticalOptions;


	IReadOnlyList<IVisualTreeElement> IVisualTreeElement.GetVisualChildren()
	{
		try
		{
			return Content is null ? [] : [Content];
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}

		return [];
	}
}