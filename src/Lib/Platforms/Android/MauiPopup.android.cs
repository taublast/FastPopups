using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using AView = Android.Views.View;

namespace AppoMobi.Maui.Popups;

#if ANDROID
/// <summary>
/// The native implementation of Popup control.
/// </summary>
public partial class MauiPopup : Dialog, IDialogInterfaceOnCancelListener
{
	readonly IMauiContext mauiContext;
	AView? overlay;

	/// <summary>
	/// The native fullscreen overlay
	/// </summary>
	public AView? Overlay
	{
		get
        {
			return overlay;
		}
	}

	/// <summary>
	/// Constructor of <see cref="MauiPopup"/>.
	/// </summary>
	/// <param name="context">An instance of <see cref="Context"/>.</param>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
	public MauiPopup(Context context, IMauiContext mauiContext)
		: base(context, Android.Resource.Style.ThemeTranslucentNoTitleBarFullScreen)
	{
		RequestWindowFeature((int)WindowFeatures.NoTitle);
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

	/// <summary>
	/// Switch fullscreen mode on/off for native Dialog
	/// </summary>
	/// <param name="value"></param>
	public void SetFullScreen(bool value)
	{
		if (Window != null)
		{
			if (value)
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11+
				{
					Window.SetDecorFitsSystemWindows(false);

					var insetsController = Window.InsetsController;
					if (insetsController != null)
					{
						insetsController.Hide(WindowInsets.Type.StatusBars());
						insetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
					}
				}
				else
				{
					Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
					Window.AddFlags(WindowManagerFlags.Fullscreen);
					Window.AddFlags(WindowManagerFlags.LayoutInScreen);
					Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
				}
			}
			else
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.R)  
				{
					Window.SetDecorFitsSystemWindows(true);

					var insetsController = Window.InsetsController;
					if (insetsController != null)
					{
						insetsController.Show(WindowInsets.Type.StatusBars());
					}
				}
				else
				{
					Window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
					Window.ClearFlags(WindowManagerFlags.Fullscreen);
					Window.ClearFlags(WindowManagerFlags.LayoutInScreen);
					Window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
				}
			}
		}
	}

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public AView? SetElement(IPopup? element)
	{
		ArgumentNullException.ThrowIfNull(element);

		VirtualView = element;

		if (TryCreateContent(VirtualView, out var container))
		{
			// Android fix: Create composite content with overlay, similar to Windows approach
			var compositeContainer = CreateCompositePopupContent(container);
			SetContentView(compositeContainer);

			SubscribeEvents();
			return container; // Return original container for reference
		}

		return container;
	}

	/// <summary>
	/// Creates a composite popup content that includes both the full-screen overlay and the actual popup content.
	/// This ensures the overlay is part of the dialog and can darken existing popups.
	/// The actual content is positioned within the fullscreen container using PopupLayoutCalculator.
	/// </summary>
	/// <param name="actualContent">The actual popup content.</param>
	/// <returns>A FrameLayout containing both overlay and positioned content.</returns>
	AView CreateCompositePopupContent(AView actualContent)
	{
		// Create a full-screen container
		var container = new FrameLayout(Context)
		{
			LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent)
		};

		if (VirtualView == null)
		{ 
			return container; 
		}

		// Create the full-screen overlay that will darken everything behind this dialog
		overlay = new AView(Context)
		{
			LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent)
		};
		var color = ((Popup)VirtualView).BackgroundColor ?? Colors.Transparent;
		overlay.SetBackgroundColor(color.ToPlatform());

		// Add click handler for dismissing popup when tapping outside
		if (VirtualView.CloseWhenBackgroundIsClicked)
		{
			overlay.Click += (sender, e) =>
			{
				VirtualView?.OnDismissedByTappingOutsideOfPopup();
			};
		}

		// Add overlay first (behind content)
		container.AddView(overlay);

		// Position the actual content within the fullscreen container
		PositionContentInContainer(container, actualContent);

		return container;
	}

	/// <summary>
	/// Positions the actual popup content within the fullscreen container using PopupLayoutCalculator.
	/// </summary>
	/// <param name="container">The fullscreen container.</param>
	/// <param name="actualContent">The actual popup content to position.</param>
	void PositionContentInContainer(FrameLayout container, AView actualContent)
	{
		if (VirtualView == null || Context == null) return;

		// Get screen dimensions
		var windowManager = Context.GetSystemService(Context.WindowService) as IWindowManager;
		if (windowManager == null) return;

		var screenSize = GetScreenSize(windowManager, VirtualView.IgnoreSafeArea);
		var parentBounds = new Rect(0, 0, screenSize.Width, screenSize.Height);

		// Get safe area insets if needed (in pixels for Android)
		var safeAreaInsets = new Microsoft.Maui.Thickness();
		if (!VirtualView.IgnoreSafeArea)
		{
			var statusBarHeight = GetStatusBarHeight(Context);
			var navigationBarHeight = GetNavigationBarHeight(windowManager);
			safeAreaInsets = new Microsoft.Maui.Thickness(0, statusBarHeight, 0, navigationBarHeight);
		}

		// Always measure the actual content to get its natural size
		actualContent.Measure(
			Android.Views.View.MeasureSpec.MakeMeasureSpec((int)screenSize.Width, MeasureSpecMode.AtMost),
			Android.Views.View.MeasureSpec.MakeMeasureSpec((int)screenSize.Height, MeasureSpecMode.AtMost));
		
		var contentSize = new Size(actualContent.MeasuredWidth, actualContent.MeasuredHeight);
		
		// If content has no natural size, fall back to layout calculator
		if (contentSize.Width == 0 || contentSize.Height == 0)
		{
			contentSize = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, safeAreaInsets);
		}

		// Calculate position based on popup alignment or anchor
		double x, y;
		if (VirtualView.Anchor != null)
		{
			// Handle anchored positioning
			var anchorBounds = GetAnchorBounds(VirtualView.Anchor);
			(x, y) = PopupLayoutCalculator.CalculateAnchoredPosition(VirtualView, contentSize, anchorBounds, parentBounds);
		}
		else
		{
			// Handle regular alignment-based positioning
			(x, y) = PopupLayoutCalculator.CalculatePosition(VirtualView, contentSize, parentBounds, safeAreaInsets);
		}

		// Create layout parameters with calculated position and size
		var layoutParams = new FrameLayout.LayoutParams((int)contentSize.Width, (int)contentSize.Height)
		{
			LeftMargin = (int)x,
			TopMargin = (int)y
		};

		actualContent.LayoutParameters = layoutParams;
		container.AddView(actualContent);
	}

	/// <summary>
	/// Gets the bounds of the anchor view in screen coordinates.
	/// </summary>
	/// <param name="anchor">The anchor view.</param>
	/// <returns>The bounds of the anchor view.</returns>
	Rect GetAnchorBounds(IView anchor)
	{
		if (anchor.Handler?.PlatformView is not AView anchorView)
		{
			return new Rect(0, 0, 100, 50); // Default if anchor not found
		}

		var locationOnScreen = new int[2];
		anchorView.GetLocationOnScreen(locationOnScreen);
		
		return new Rect(locationOnScreen[0], locationOnScreen[1], anchorView.Width, anchorView.Height);
	}

	/// <summary>
	/// Gets the screen size, optionally accounting for safe areas.
	/// </summary>
	/// <param name="windowManager">The window manager.</param>
	/// <param name="ignoresSafeArea">Whether to ignore safe areas.</param>
	/// <returns>The screen size.</returns>
	Size GetScreenSize(IWindowManager windowManager, bool ignoresSafeArea)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			return new Size(windowMetrics.Bounds.Width(), windowMetrics.Bounds.Height());
		}
		else if (windowManager.DefaultDisplay != null)
		{
			var realSize = new Android.Graphics.Point();
			windowManager.DefaultDisplay.GetRealSize(realSize);
			return new Size(realSize.X, realSize.Y);
		}
		
		return new Size(1000, 1000); // Fallback
	}

	/// <summary>
	/// Gets the status bar height.
	/// </summary>
	/// <param name="context">The context.</param>
	/// <returns>The status bar height in pixels.</returns>
	static int GetStatusBarHeight(Context context)
	{
		if (context.Resources != null)
		{
			int resourceId = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
			if (resourceId > 0)
			{
				return context.Resources.GetDimensionPixelSize(resourceId);
			}
		}
		return 0;
	}

	/// <summary>
	/// Gets the navigation bar height.
	/// </summary>
	/// <param name="windowManager">The window manager.</param>
	/// <returns>The navigation bar height in pixels.</returns>
	static int GetNavigationBarHeight(IWindowManager windowManager)
	{
		int heightPixels = 0;
		
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			var windowWidth = windowMetrics.Bounds.Width();
			var windowHeight = windowMetrics.Bounds.Height();
			heightPixels = windowHeight < windowWidth ? windowInsets.Left + windowInsets.Right : windowInsets.Bottom;
		}
		else if (windowManager.DefaultDisplay != null)
		{
			var realSize = new Android.Graphics.Point();
			var displaySize = new Android.Graphics.Point();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			windowManager.DefaultDisplay.GetSize(displaySize);

			heightPixels = realSize.Y < realSize.X
				? (realSize.X - displaySize.X)
				: (realSize.Y - displaySize.Y);
		}

		return heightPixels;
	}

	/// <summary>
	/// Method to show the Popup.
	/// </summary>
	public override void Show()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");

		// Show the dialog popup - the overlay is now part of the dialog content
		// so it will automatically darken everything that was visible before this dialog opened
		base.Show();

		VirtualView.OnOpened();
	}

	/// <summary>
	/// Is dismissing the Popup.
	/// </summary>
	public override void Dismiss()
	{
		// No need to manually remove overlay since it's part of the dialog content
		// and will be cleaned up automatically when the dialog is dismissed
		base.Dismiss();
	}

	/// <summary>
	/// Method triggered when the Popup is dismissed by tapping outside of the Popup.
	/// </summary>
	/// <param name="dialog">An instance of the <see cref="IDialogInterface"/>.</param>
	public void OnDismissedByTappingOutsideOfPopup(IDialogInterface dialog)
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");
		_ = VirtualView.Handler ?? throw new InvalidOperationException($"{nameof(VirtualView.Handler)} cannot be null");

		VirtualView.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
	}

	/// <summary>
	/// Method to clean up the resources of the <see cref="MauiPopup"/>.
	/// </summary>
	public void CleanUp()
	{
		VirtualView = null;
		overlay = null;
	}

	/// <inheritdoc/>
	public override bool OnTouchEvent(MotionEvent e)
	{
		if (VirtualView is not null)
		{
			if (VirtualView.CloseWhenBackgroundIsClicked &&
				e.Action == MotionEventActions.Up)
			{
				if (Window?.DecorView is AView decorView)
				{
					float x = e.GetX();
					float y = e.GetY();

					if (!(x >= 0 && x <= decorView.Width && y >= 0 && y <= decorView.Height))
					{
						if (IsShowing)
						{
							OnDismissedByTappingOutsideOfPopup(this);
						}
					}
				}
			}
		}

		return !this.IsDisposed() && base.OnTouchEvent(e);
	}

    public bool IsDisposed()
    {
        if (this is Java.Lang.Object obj)
        {
            return obj.Handle == IntPtr.Zero;
        }
        return false;
    }

    bool TryCreateContent(in IPopup popup, [NotNullWhen(true)] out AView? container)
	{
		container = null;

		if (popup.Content is null)
		{
			return false;
		}

		container = popup.Content.ToPlatform(mauiContext);
		// Don't call SetContentView here - it will be called in SetElement with composite content

		return true;
	}

	void SubscribeEvents()
	{
		SetOnCancelListener(this);
	}

	void IDialogInterfaceOnCancelListener.OnCancel(IDialogInterface? dialog) => OnDismissedByTappingOutsideOfPopup(this);
}

#endif