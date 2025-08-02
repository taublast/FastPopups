using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using System.Diagnostics.CodeAnalysis;
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
    public MauiPopup(Context context, IMauiContext mauiContext, bool ignoreSafeArea)
		: base(context, GetDialogTheme(ignoreSafeArea))
    {
		RequestWindowFeature((int)WindowFeatures.NoTitle);
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

    static int GetDialogTheme(bool ignoreSafeArea)
    {
        return ignoreSafeArea
            ? Android.Resource.Style.ThemeTranslucentNoTitleBarFullScreen
            : Android.Resource.Style.ThemeTranslucentNoTitleBar; // or another appropriate theme
    }

    /// <summary>
    /// Switch fullscreen mode on/off for native Dialog with enhanced compatibility
    /// </summary>
    /// <param name="value"></param>
    public void SetFullScreen(bool value)
    {
        if (Window != null)
        {
            if (value)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    Window.SetDecorFitsSystemWindows(false);
                    var insetsController = Window.InsetsController;
                    if (insetsController != null)
                    {
                        insetsController.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                        insetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    }
                    Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                }
                else
                {
                    Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                    Window.AddFlags(WindowManagerFlags.Fullscreen);
                    Window.AddFlags(WindowManagerFlags.LayoutInScreen);
                    Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);

                    // Handle display cutout for devices with notches (API 28+)
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                    {
                        var layoutParams = Window.Attributes;
                        layoutParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                        Window.Attributes = layoutParams;
                    }
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
                        insetsController.Show(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                    }
                    Window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
                }
                else
                {
                    Window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
                    Window.ClearFlags(WindowManagerFlags.Fullscreen);
                    Window.ClearFlags(WindowManagerFlags.LayoutInScreen);
                    Window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
                    Window.AddFlags(WindowManagerFlags.ForceNotFullscreen);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                    {
                        var layoutParams = Window.Attributes;
                        layoutParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Default;
                        Window.Attributes = layoutParams;
                    }
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

		// Get safe area insets in DIPs for use with shared layout logic
		var safeAreaInsets = new Microsoft.Maui.Thickness();
		if (!VirtualView.IgnoreSafeArea)
		{
			var statusBarHeight = GetStatusBarHeight(Context);
			var navigationBarHeight = GetNavigationBarHeight(windowManager);
			safeAreaInsets = new Microsoft.Maui.Thickness(0, statusBarHeight, 0, navigationBarHeight);
		}

		// Always measure the actual content to get its natural size (Android measure uses pixels)
		actualContent.Measure(
			Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(screenSize.Width), MeasureSpecMode.AtMost),
			Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(screenSize.Height), MeasureSpecMode.AtMost));
		
		// Convert measured size from pixels to DIPs for use with shared layout logic
		var contentSize = new Size(
			PixelsToDips(actualContent.MeasuredWidth), 
			PixelsToDips(actualContent.MeasuredHeight));
		
		// If content has no natural size, fall back to layout calculator
		if (contentSize.Width == 0 || contentSize.Height == 0)
		{
			contentSize = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, safeAreaInsets);
		}

		// Calculate position based on popup alignment or anchor (all in DIPs)
		double x, y;
		if (VirtualView.Anchor != null)
		{
			// Get anchor bounds and calculate position using shared logic (all in DIPs)
			var anchorBounds = GetAnchorBounds(VirtualView.Anchor);
			(x, y) = PopupLayoutCalculator.CalculateAnchoredPosition(VirtualView, contentSize, anchorBounds, parentBounds);
			
			System.Diagnostics.Debug.WriteLine($"[ANCHOR DEBUG] Anchor bounds (DIPs): {anchorBounds}");
			System.Diagnostics.Debug.WriteLine($"[ANCHOR DEBUG] Calculated position (DIPs): X={x}, Y={y}");
			System.Diagnostics.Debug.WriteLine($"[ANCHOR DEBUG] Content size (DIPs): {contentSize}");
		}
		else
		{
			// Handle regular alignment-based positioning (all in DIPs)
			(x, y) = PopupLayoutCalculator.CalculatePosition(VirtualView, contentSize, parentBounds, safeAreaInsets);
		}

		// Convert final position and size from DIPs to pixels for Android layout parameters
		var layoutParams = new FrameLayout.LayoutParams(
			DipsToPixels(contentSize.Width), 
			DipsToPixels(contentSize.Height))
		{
			LeftMargin = DipsToPixels(x),
			TopMargin = DipsToPixels(y)
		};
		
		System.Diagnostics.Debug.WriteLine($"[LAYOUT DEBUG] Final layout (pixels): X={DipsToPixels(x)}, Y={DipsToPixels(y)}, W={DipsToPixels(contentSize.Width)}, H={DipsToPixels(contentSize.Height)}");
		System.Diagnostics.Debug.WriteLine($"[LAYOUT DEBUG] Density factor: {GetDensity()}");

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

		// Get density factor to convert pixels to DIPs
		var density = Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;

		// Get anchor coordinates relative to dialog window
		var windowLocationOnScreen = new int[2];
		var anchorLocationOnScreen = new int[2];
		
		// Get our dialog window position
		if (Window?.DecorView != null)
		{
			Window.DecorView.GetLocationOnScreen(windowLocationOnScreen);
		}
		
		// Get anchor position
		anchorView.GetLocationOnScreen(anchorLocationOnScreen);
		
		System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Dialog DecorView screen position (pixels): X={windowLocationOnScreen[0]}, Y={windowLocationOnScreen[1]}");
		System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Anchor view screen position (pixels): X={anchorLocationOnScreen[0]}, Y={anchorLocationOnScreen[1]}");
		
		// Calculate relative position (anchor relative to our dialog window) in pixels
		var relativeXPixels = anchorLocationOnScreen[0] - windowLocationOnScreen[0];
		var relativeYPixels = anchorLocationOnScreen[1] - windowLocationOnScreen[1];
		
		System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Relative position (pixels): X={relativeXPixels}, Y={relativeYPixels}");
		System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Anchor size (pixels): W={anchorView.Width}, H={anchorView.Height}");
		
		// Convert from pixels to DIPs (density-independent pixels)
		var relativeX = relativeXPixels / density;
		var relativeY = relativeYPixels / density;
		var width = anchorView.Width / density;
		var height = anchorView.Height / density;
		
		System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Final anchor bounds (DIPs): X={relativeX}, Y={relativeY}, W={width}, H={height}");
		System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Density factor: {density}");
		
		return new Rect(relativeX, relativeY, width, height);
	}

	/// <summary>
	/// Gets the screen size in MAUI DIPs (density-independent pixels).
	/// </summary>
	/// <param name="windowManager">The window manager.</param>
	/// <param name="ignoresSafeArea">Whether to ignore safe areas.</param>
	/// <returns>The screen size in DIPs.</returns>
	Size GetScreenSize(IWindowManager windowManager, bool ignoresSafeArea)
	{
		var density = GetDensity();
		
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			// Convert from pixels to DIPs
			return new Size(
				windowMetrics.Bounds.Width() / density, 
				windowMetrics.Bounds.Height() / density);
		}
		else if (windowManager.DefaultDisplay != null)
		{
			var realSize = new Android.Graphics.Point();
			windowManager.DefaultDisplay.GetRealSize(realSize);
			// Convert from pixels to DIPs
			return new Size(realSize.X / density, realSize.Y / density);
		}
		
		return new Size(1000, 1000); // Fallback
	}

	/// <summary>
	/// Gets the status bar height in MAUI DIPs (density-independent pixels).
	/// </summary>
	/// <param name="context">The context.</param>
	/// <returns>The status bar height in DIPs.</returns>
	double GetStatusBarHeight(Context context)
	{
		if (context.Resources != null)
		{
			int resourceId = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
			if (resourceId > 0)
			{
				var pixelSize = context.Resources.GetDimensionPixelSize(resourceId);
				var density = GetDensity();
				// Convert from pixels to DIPs
				return pixelSize / density;
			}
		}
		return 0;
	}

	/// <summary>
	/// Gets the navigation bar height in MAUI DIPs (density-independent pixels).
	/// </summary>
	/// <param name="windowManager">The window manager.</param>
	/// <returns>The navigation bar height in DIPs.</returns>
	double GetNavigationBarHeight(IWindowManager windowManager)
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

		var density = GetDensity();
		// Convert from pixels to DIPs
		return heightPixels / density;
	}

	/// <summary>
	/// Gets the display density factor for converting between pixels and DIPs.
	/// </summary>
	/// <returns>The density factor (pixels per DIP).</returns>
	float GetDensity()
	{
		return Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;
	}

	/// <summary>
	/// Converts DIPs to pixels for use with Android layout parameters.
	/// </summary>
	/// <param name="dips">Value in DIPs.</param>
	/// <returns>Value in pixels.</returns>
	int DipsToPixels(double dips)
	{
		return (int)(dips * GetDensity());
	}

	/// <summary>
	/// Converts pixels to DIPs for use with MAUI layout calculations.
	/// </summary>
	/// <param name="pixels">Value in pixels.</param>
	/// <returns>Value in DIPs.</returns>
	double PixelsToDips(int pixels)
	{
		return pixels / GetDensity();
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