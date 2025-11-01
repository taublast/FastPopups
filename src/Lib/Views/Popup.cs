using System;
using System.Diagnostics;
using AppoMobi.Maui.FastPopups;
using View = Microsoft.Maui.Controls.View;

namespace FastPopups;

/// <summary>
/// Represents a small View that pops up at front the Page. Implements <see cref="IPopup"/>.
/// </summary>
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
	/// Backing BindableProperty for the <see cref="IsFullScreen"/> property.
	/// </summary>
	public static readonly BindableProperty IsFullScreenProperty =
		BindableProperty.Create(nameof(IsFullScreen), typeof(bool), typeof(Popup), false);


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
		BindableProperty.Create(nameof(AnimationType), typeof(PopupAnimationType), typeof(Popup), PopupAnimationType.Default);

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
	/// Controls whether the popup should respect safe insets like status bar etc. 
	/// </summary>
	public bool IsFullScreen
	{
		get => (bool)GetValue(IsFullScreenProperty);
		set => SetValue(IsFullScreenProperty, value);
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
	/// The overlay fade always uses linear easing regardless of this setting.
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

		RemoveBinding(Popup.IsFullScreenProperty);
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