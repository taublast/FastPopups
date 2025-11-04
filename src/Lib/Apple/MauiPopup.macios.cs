#if MACCATALYST || IOS

using System.Diagnostics.CodeAnalysis;
using AppoMobi.Maui.FastPopups;
using FastPopups.Extensions;
using FastPopups.Platforms.iOS;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace FastPopups;



/// <summary>
/// The native implementation of Popup control.
/// </summary>
/// <remarks>
/// Constructor of <see cref="MauiPopup"/>.
/// </remarks>
/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
public partial class MauiPopup(IMauiContext mauiContext) : UIViewController
{

	internal bool CanBeDismissedByTappingInternal;
	readonly IMauiContext mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	readonly PopupAnimator animator = new PopupAnimator();
	bool _appeared = false;
	bool _isAnimating = false;
	UIView? overlay;
	UIView? _contentView;
	PopupDisplayMode _displayMode = PopupDisplayMode.Default;

	/// <summary>
	/// The native fullscreen overlay
	/// </summary>
	public UIView? Overlay
	{
		get
		{
			return overlay;
		}
	}

	/// <summary>
	/// An instance of the <see cref="PageHandler"/> that holds the <see cref="IPopup.Content"/>.
	/// </summary>
	public PageHandler? Control { get; private set; }

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	internal UIViewController? ViewController { get; private set; }

	/// <summary>
	/// Method to update the Popup's size.
	/// </summary>
	/// <param name="size"></param>
	public void SetElementSize(Size size) =>
		Control?.ContainerView?.SizeThatFits(size);

	/// <summary>
	/// Gets a value indicating whether the status bar should be hidden.
	/// </summary>
	public override bool PrefersStatusBarHidden() => _displayMode == PopupDisplayMode.FullScreen;

	/// <summary>
	/// Updates the display mode and refreshes the status bar appearance.
	/// </summary>
	/// <param name="displayMode">The new display mode.</param>
	public void SetDisplayMode(PopupDisplayMode displayMode)
	{
		if (_displayMode != displayMode)
		{
			_displayMode = displayMode;
			SetNeedsStatusBarAppearanceUpdate();
		}
	}

	/// <inheritdoc/>
	public override void ViewDidLoad()
	{
		base.ViewDidLoad();

		// Set transparent background to avoid white background behind rounded corners
		if (View != null)
		{
			View.BackgroundColor = UIColor.Clear;
		}
	}

	/// <inheritdoc/>
	public override void ViewDidLayoutSubviews()
	{
		base.ViewDidLayoutSubviews();

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null.");
		_ = View.Superview ?? throw new InvalidOperationException($"{nameof(View.Superview)} cannot be null.");

		View.Superview.Layer.CornerRadius = 0.0f;
		View.Superview.Layer.MasksToBounds = false;

		SetElementSize(new Size(View.Bounds.Width, View.Bounds.Height));

		// CRITICAL: Do NOT reposition during HIDE animation - it overrides transforms!
		if (VirtualView is not null && !_isAnimating)
		{
			this.SetSize(VirtualView);
			this.SetLayout(VirtualView);
		}

		// Trigger animation after layout is complete and view has proper size
		if (!_appeared && _contentView != null && overlay != null && VirtualView is Popup popup)
		{
			if (_contentView.Bounds.Width > 0 && _contentView.Bounds.Height > 0)
			{
				_appeared = true;
				if (popup.AnimationType != PopupAnimationType.None)
				{
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						_isAnimating = true;
						try
						{
							animator.PrepareShowAnimation(_contentView, overlay, popup.AnimationType,
								popup.AnimationDuration, popup.AnimationEasing);
							await animator.AnimateShowAsync(_contentView, overlay, popup.AnimationType,
								popup.AnimationDuration, popup.AnimationEasing);
							popup.OnOpened();
						}
						finally
						{
							_isAnimating = false;
						}
					});
				}
				else
				{
					popup.OnOpened();
				}
			}
		}
	}

	/// <inheritdoc/>
	public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
	{
		coordinator.AnimateAlongsideTransition(_ =>
		{
			// Before screen rotate
		}, _ =>
		{
			// After screen rotate
			if (VirtualView is not null)
			{
				this.SetSize(VirtualView);
				this.SetLayout(VirtualView);
			}
		});

		if (View is not null)
		{
			View.Bounds = new CGRect(0, 0, PreferredContentSize.Width, PreferredContentSize.Height);
		}

		base.ViewWillTransitionToSize(toSize, coordinator);
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	[MemberNotNull(nameof(VirtualView), nameof(ViewController))]
	public void SetElement(IPopup element)
	{
#if MACCATALYST
		if (element.Parent?.Handler is not PageHandler)
		{
			throw new InvalidOperationException($"The {nameof(element.Parent)} must be of type {typeof(PageHandler)}.");
		}
#endif

		VirtualView = element;
		ModalPresentationStyle = UIModalPresentationStyle.OverFullScreen;
		ModalTransitionStyle = UIModalTransitionStyle.CrossDissolve;

		// CRITICAL: This allows the presented view controller to control status bar appearance
		ModalPresentationCapturesStatusBarAppearance = true;

		// Set initial display mode
		_displayMode = element.DisplayMode;

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null.");
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");

#if MACCATALYST
		var pageHandler = VirtualView.Parent.Handler as PageHandler;
		var rootViewController = pageHandler?.ViewController ?? WindowStateManager.Default.GetCurrentUIViewController() ?? throw new InvalidOperationException($"{nameof(PageHandler.ViewController)} cannot be null.");
#else
		var rootViewController = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new InvalidOperationException($"{nameof(PageHandler.ViewController)} cannot be null.");
#endif

		ViewController ??= rootViewController;
	}

	/// <summary>
	/// Method to clean up the resources of the <see cref="MauiPopup"/>.
	/// </summary>
	public void CleanUp()
	{
		if (VirtualView is null)
		{
			return;
		}

		VirtualView = null;

		if (PresentationController is UIPopoverPresentationController presentationController)
		{
			presentationController.Delegate = null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="func"></param>
	/// <param name="virtualView"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(Control), nameof(ViewController))]
	public void CreateControl(Func<IPopup, PageHandler> func, in IPopup virtualView)
	{
		Control = func(virtualView);

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null.");
		SetView(View, Control);

		_ = ViewController ?? throw new InvalidOperationException($"{nameof(ViewController)} cannot be null.");

		// Store content view reference for animation
		_contentView = Control.ViewController?.View;

		AddToCurrentPageViewController(ViewController);

		if (virtualView.Handler != null)
		{
			overlay = new UIView(this.View.Bounds)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
				BackgroundColor = ((Popup)virtualView).BackgroundColor.ToPlatform()
			};

			if (virtualView.CloseWhenBackgroundIsClicked)
			{
				var tapGesture = new UITapGestureRecognizer(tapEvent =>
				{
					if (CanBeDismissedByTappingInternal && VirtualView is Popup popup && popup.ShouldDismissOnOutsideClick())
					{
						// Check if tap is actually outside content area (iOS gesture fix)
						var tapLocation = tapEvent.LocationInView(this.View);
						var contentView = Control?.ViewController?.View;
						if (contentView != null)
						{
							var contentBounds = contentView.Frame;
							if (contentBounds.Contains(tapLocation))
								return; // Don't dismiss if tap is on content
						}

						// DON'T dismiss here - let the handler flow run the animation first
						// The actual dismissal will happen in MapOnClosed after animation completes
						_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");
						VirtualView.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
					}
				})
				{
					CancelsTouchesInView = false
				};
				View.AddGestureRecognizer(tapGesture);
			}

			this.View.InsertSubview(overlay, 0);
		}

		this.SetSize(virtualView);
		this.SetLayout(virtualView);
	}

	void SetView(UIView view, IPlatformViewHandler control)
	{
		view.AddSubview(control.ViewController?.View ?? throw new InvalidOperationException($"{nameof(control.ViewController.View)} cannot be null."));
		view.Bounds = new CGRect(0, 0, PreferredContentSize.Width, PreferredContentSize.Height);
		AddChildViewController(control.ViewController);

		view.SafeTopAnchor().ConstraintEqualTo(control.ViewController.View.SafeTopAnchor()).Active = true;
		view.SafeBottomAnchor().ConstraintEqualTo(control.ViewController.View.SafeBottomAnchor()).Active = true;
		view.SafeLeadingAnchor().ConstraintEqualTo(control.ViewController.View.SafeLeadingAnchor()).Active = true;
		view.SafeTrailingAnchor().ConstraintEqualTo(control.ViewController.View.SafeTrailingAnchor()).Active = true;

	}

	/// <summary>
	/// Presents this popup view controller modally over the specified view controller.
	/// </summary>
	/// <param name="viewController">The view controller that will present this popup.</param>
	/// <remarks>
	/// This method uses <see cref="UIModalPresentationStyle.OverFullScreen"/> to present
	/// the popup over the entire screen with a transparent background, allowing the underlying
	/// content to remain visible through the overlay.
	/// Presents without animation (animated: false) because our custom animation
	/// runs in ViewDidLayoutSubviews after the view has proper size.
	/// </remarks>
	public void AddToCurrentPageViewController(UIViewController viewController)
	{
		// Present WITHOUT iOS animation - our custom animation will run in ViewDidLayoutSubviews
		viewController.PresentViewController(this, false, null);
	}

	/// <summary>
	/// Closes the popup with animation.
	/// </summary>
	public async Task CloseWithAnimationAsync()
	{
		if (_contentView != null && overlay != null && VirtualView is Popup popup)
		{
			if (popup.AnimationType != PopupAnimationType.None)
			{
				_isAnimating = true;
				try
				{
					await animator.AnimateHideAsync(_contentView, overlay, popup.AnimationType,
						popup.AnimationDuration, popup.AnimationEasing);
				}
				finally
				{
					_isAnimating = false;
				}
			}
		}
	}

}

#endif