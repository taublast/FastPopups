using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.Window.Layout;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using System.Diagnostics.CodeAnalysis;
using static AndroidX.ViewPager.Widget.ViewPager;
using AView = Android.Views.View;

namespace FastPopups;

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
        get { return overlay; }
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
                        insetsController.SystemBarsBehavior =
                            (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    }
                }
                else
                {
                    Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                    Window.AddFlags(WindowManagerFlags.Fullscreen);
                    Window.AddFlags(WindowManagerFlags.LayoutInScreen);
                    Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);

    
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
                Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                Window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                var layoutParams = Window.Attributes;

                // CRITICAL: Allow drawing in cutout area with ShortEdges
                layoutParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;

                // For API 30+, also try the newer Always mode for maximum coverage
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    try
                    {
                        // This allows drawing even in the cutout area itself
                        layoutParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Always;
                    }
                    catch
                    {
                        // Fall back to ShortEdges if Always is not supported
                        layoutParams.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                    }
                }

                Window.Attributes = layoutParams;
                System.Diagnostics.Debug.WriteLine(
                    $"[CUTOUT FULLSCREEN] Set cutout mode: {layoutParams.LayoutInDisplayCutoutMode}");
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

            var compositeContainer = CreateCompositePopupContent(container);
            SetContentView(compositeContainer);

            SubscribeEvents();
            return container; // Return original container for reference
        }

        return container;
    }

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


        if (VirtualView.CloseWhenBackgroundIsClicked)
        {
            overlay.Click += (sender, e) =>
            {
                if (VirtualView is Popup popup && popup.ShouldDismissOnOutsideClick())
                {
                    VirtualView?.OnDismissedByTappingOutsideOfPopup();
                }
            };
        }

        // Add overlay first (behind content)
        container.AddView(overlay);

        // Position the actual content within the fullscreen container
        PositionContentInContainer(container, actualContent);

        return container;
    }

    bool HasNavBar()
    {
        var hasBackKey = KeyCharacterMap.DeviceHasKey(Keycode.Back);
        var hasHomeKey = KeyCharacterMap.DeviceHasKey(Keycode.Home);

        if (hasBackKey && hasHomeKey)
        {
            // no navigation bar, unless it is enabled in the settings
            return false;
        }
        else
        {
            // 99% sure there's a navigation bar
            return true;
        }
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

        var screenSize = GetScreenSize(windowManager);
        var parentBounds = new Rect(0, 0, screenSize.Width, screenSize.Height);

        //// Get safe area insets in DIPs for use with shared layout logic
        var safeAreaInsets = new Microsoft.Maui.Thickness();

        var navigationBarHeight = GetNavigationBarHeight(windowManager);
        var statusBarHeight = GetStatusBarHeight(Context);

        var offsetY = 0.0;

        bool hasCutout = false;


        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var windowMetrics = windowManager.CurrentWindowMetrics;
            var insets = windowMetrics.WindowInsets;
            var cutout = insets.DisplayCutout;
            var density = GetDensity();
            if (cutout != null)
            {
                statusBarHeight = Math.Max(statusBarHeight, cutout.SafeInsetTop / density);
                offsetY = statusBarHeight;
                hasCutout = true;
            }

            if (!VirtualView.IsFullScreen)
            {
                if (hasCutout)
                {
                    safeAreaInsets =
                        new Microsoft.Maui.Thickness(0, 0, 0, navigationBarHeight + statusBarHeight);
                }
                else
                {
                    safeAreaInsets =
                        new Microsoft.Maui.Thickness(0, statusBarHeight, 0, navigationBarHeight);
                }

                offsetY = statusBarHeight;
            }
            else if (hasCutout)
            {
                var hasNabBar = HasNavBar();

                if (hasNabBar)
                {
                    safeAreaInsets =
                        new Microsoft.Maui.Thickness(0, statusBarHeight + navigationBarHeight, 0, 0);
                }
                else
                {
                    safeAreaInsets =
                        new Microsoft.Maui.Thickness(0, statusBarHeight, 0, 0);
                }
            }

            if (VirtualView.IsFullScreen)
            {
                parentBounds = new(
                    parentBounds.Left + safeAreaInsets.Left,
                    parentBounds.Top + safeAreaInsets.Top - offsetY,
                    parentBounds.Width - safeAreaInsets.HorizontalThickness,
                    parentBounds.Height - safeAreaInsets.VerticalThickness + offsetY);
            }
            else
            {
                parentBounds = new(
                    parentBounds.Left + safeAreaInsets.Left,
                    parentBounds.Top + safeAreaInsets.Top,
                    parentBounds.Width - safeAreaInsets.HorizontalThickness,
                    parentBounds.Height - safeAreaInsets.VerticalThickness);
            }
        }
        else
        {
            if (!VirtualView.IsFullScreen)
            {
                //offsetY = statusBarHeight;

                safeAreaInsets =
                    new Microsoft.Maui.Thickness(0, statusBarHeight, 0, navigationBarHeight - statusBarHeight);
            }

            if (VirtualView.IsFullScreen)
            {
                parentBounds = new(
                    parentBounds.Left + safeAreaInsets.Left,
                    parentBounds.Top + safeAreaInsets.Top - offsetY,
                    parentBounds.Width - safeAreaInsets.HorizontalThickness,
                    parentBounds.Height - safeAreaInsets.VerticalThickness + offsetY);
            }
            else
            {
                parentBounds = new(
                    parentBounds.Left + safeAreaInsets.Left,
                    parentBounds.Top + safeAreaInsets.Top,
                    parentBounds.Width - safeAreaInsets.HorizontalThickness,
                    parentBounds.Height - safeAreaInsets.VerticalThickness);
            }
        }


        // Check if popup has explicit size requests first
        // Cast to VisualElement to access HeightRequest/WidthRequest properties
        var visualElement = VirtualView as VisualElement;
        var hasExplicitWidth = visualElement?.WidthRequest > 0;
        var hasExplicitHeight = visualElement?.HeightRequest > 0;

        // For Fill layouts, we need to calculate size based on layout options, not measured content
        // Check if we have Fill layout options first
        var horizontalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.HorizontalOptions);
        var verticalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.VerticalOptions);
        var isFillWidth = horizontalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
        var isFillHeight = verticalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

        Size contentSize;

        // If popup has explicit width and height requests, use them (similar to Apple implementation)
        if (hasExplicitWidth == true && hasExplicitHeight == true && visualElement != null)
        {
            contentSize = new Size(
                Math.Min(visualElement.WidthRequest, parentBounds.Width),
                Math.Min(visualElement.HeightRequest, parentBounds.Height));
        }
        else if (hasExplicitWidth == true || hasExplicitHeight == true)
        {
            // Mixed case: some explicit, some calculated (similar to Apple implementation)
            double width, height;

            if (hasExplicitWidth == true && visualElement != null)
            {
                width = Math.Min(visualElement.WidthRequest, parentBounds.Width);
            }
            else if (isFillWidth)
            {
                width = parentBounds.Width;
            }
            else
            {
                // Measure content for width
                actualContent.Measure(
                    Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(parentBounds.Width),
                        MeasureSpecMode.AtMost),
                    Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(parentBounds.Height),
                        MeasureSpecMode.AtMost));
                width = PixelsToDips(actualContent.MeasuredWidth);
                if (width == 0)
                {
                    width = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, Thickness.Zero).Width;
                }
            }

            if (hasExplicitHeight == true && visualElement != null)
            {
                height = Math.Min(visualElement.HeightRequest, parentBounds.Height);
            }
            else if (isFillHeight)
            {
                height = parentBounds.Height;
            }
            else
            {
                // Measure content for height - we need to re-measure with the determined width
                // to get the correct height, especially when we have an explicit width
                actualContent.Measure(
                    Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(width),
                        MeasureSpecMode.Exactly),
                    Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(parentBounds.Height),
                        MeasureSpecMode.AtMost));

                height = PixelsToDips(actualContent.MeasuredHeight);
                if (height == 0)
                {
                    height = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, Thickness.Zero).Height;
                }
            }

            contentSize = new Size(width, height);
        }
        else if (isFillWidth || isFillHeight)
        {
            // For Fill layouts, use the layout calculator to get proper Fill sizing
            contentSize = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, Thickness.Zero);
        }
        else
        {
            // For non-Fill layouts with no explicit requests, measure the actual content to get its natural size (Android measure uses pixels)
            actualContent.Measure(
                Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(parentBounds.Width),
                    MeasureSpecMode.AtMost),
                Android.Views.View.MeasureSpec.MakeMeasureSpec(DipsToPixels(parentBounds.Height),
                    MeasureSpecMode.AtMost));

            // Convert measured size from pixels to DIPs for use with shared layout logic
            contentSize = new Size(
                PixelsToDips(actualContent.MeasuredWidth),
                PixelsToDips(actualContent.MeasuredHeight));

            // If content has no natural size, fall back to layout calculator
            if (contentSize.Width == 0 || contentSize.Height == 0)
            {
                contentSize = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, Thickness.Zero);
            }
        }

        // Calculate position based on popup alignment or anchor (all in DIPs)
        double x, y;
        if (VirtualView.Anchor != null)
        {
            // Get anchor bounds and calculate position using shared logic (all in DIPs)
            var anchorBounds = GetAnchorBounds(VirtualView.Anchor);
            (x, y) = PopupLayoutCalculator.CalculateAnchoredPosition(VirtualView, contentSize, anchorBounds,
                parentBounds);

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                if (hasCutout)
                {
                    if (!VirtualView.IsFullScreen)
                    {
                        y -= statusBarHeight;
                    }
                }
                else
                {
                    if (VirtualView.IsFullScreen)
                    {
                        y -= statusBarHeight;
                    }
                }
            }
        }
        else
        {

            (x, y) = PopupLayoutCalculator.CalculatePosition(VirtualView, contentSize, parentBounds, Thickness.Zero);
        }

        // Apply padding offset to position content correctly within the popup
        var (paddingX, paddingY) = PopupLayoutCalculator.GetPaddingOffset(VirtualView);

        // Calculate the available content size (subtract padding from total popup size)
        var availableContentSize = PopupLayoutCalculator.ApplyPadding(VirtualView, contentSize, new Size(parentBounds.Width, parentBounds.Height));

        // Adjust coordinates for IsFullScreen=false: move TOP up and extend BOTTOM down by status bar height
        var adjustedY = y + paddingY;
        var adjustedHeight = availableContentSize.Height;

        //if (!VirtualView.IsFullScreen)
        //{
        //    var statusBarHeight = GetStatusBarHeight(Context);
        //    //adjustedY += statusBarHeight;
        //    //adjustedHeight += statusBarHeight;
        //}

        // Convert final position and size from DIPs to pixels for Android layout parameters
        var layoutParams = new FrameLayout.LayoutParams(
            DipsToPixels(availableContentSize.Width),
            DipsToPixels(adjustedHeight))
        {
            LeftMargin = DipsToPixels(x + paddingX),
            TopMargin = DipsToPixels(adjustedY)
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

        System.Diagnostics.Debug.WriteLine(
            $"[ANCHOR COORDS] Dialog DecorView screen position (pixels): X={windowLocationOnScreen[0]}, Y={windowLocationOnScreen[1]}");
        System.Diagnostics.Debug.WriteLine(
            $"[ANCHOR COORDS] Anchor view screen position (pixels): X={anchorLocationOnScreen[0]}, Y={anchorLocationOnScreen[1]}");

        // Calculate relative position (anchor relative to our dialog window) in pixels
        var relativeXPixels = anchorLocationOnScreen[0] - windowLocationOnScreen[0];
        var relativeYPixels = anchorLocationOnScreen[1] - windowLocationOnScreen[1];

        System.Diagnostics.Debug.WriteLine(
            $"[ANCHOR COORDS] Relative position (pixels): X={relativeXPixels}, Y={relativeYPixels}");
        System.Diagnostics.Debug.WriteLine(
            $"[ANCHOR COORDS] Anchor size (pixels): W={anchorView.Width}, H={anchorView.Height}");

        // Convert from pixels to DIPs (density-independent pixels)
        var relativeX = relativeXPixels / density;
        var relativeY = relativeYPixels / density;
        var width = anchorView.Width / density;
        var height = anchorView.Height / density;

        System.Diagnostics.Debug.WriteLine(
            $"[ANCHOR COORDS] Final anchor bounds (DIPs): X={relativeX}, Y={relativeY}, W={width}, H={height}");
        System.Diagnostics.Debug.WriteLine($"[ANCHOR COORDS] Density factor: {density}");

        return new Rect(relativeX, relativeY, width, height);
    }

    /// <summary>
    /// Gets the screen size in MAUI DIPs (density-independent pixels).
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    /// <param name="ignoresSafeArea">Whether to ignore safe areas.</param>
    /// <returns>The screen size in DIPs.</returns>
    Size GetScreenSize(IWindowManager windowManager)
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

        if (windowManager.DefaultDisplay != null)
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
        double heightPixels = 0;

        //if (OperatingSystem.IsAndroidVersionAtLeast(30))
        //{
        //    var windowMetrics = Window.WindowManager.CurrentWindowMetrics;
        //    var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.StatusBars());
        //    heightPixels = windowInsets.Top; // Status bar is always at the top
        //}
        //else
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
            var insets = windowMetrics.WindowInsets;
            var windowInsets = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
            var windowInsets2 = insets.HasSystemWindowInsets;

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

    void IDialogInterfaceOnCancelListener.OnCancel(IDialogInterface? dialog) =>
        OnDismissedByTappingOutsideOfPopup(this);
}

#endif