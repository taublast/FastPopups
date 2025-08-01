using Android.Content;
using Android.Views;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using AView = Android.Views.View;

namespace AppoMobi.Maui.Popups;

#if ANDROID
/// <summary>
/// Android-specific implementation of anchor bounds provider.
/// Handles Android coordinate system and density scaling properly.
/// </summary>
public class AndroidAnchorBoundsProvider : CrossPlatformAnchorCalculator.IAnchorBoundsProvider
{
    private readonly Context context;
    private readonly Android.Views.Window? dialogWindow;

    public AndroidAnchorBoundsProvider(Context context, Android.Views.Window? dialogWindow)
    {
        this.context = context;
        this.dialogWindow = dialogWindow;
    }

    public Rect GetAnchorBounds(IView anchor, IMauiContext mauiContext)
    {
        if (anchor.Handler?.PlatformView is not AView anchorView)
        {
            return new Rect(0, 0, 100, 50); // Default if anchor not found
        }

        // Use MAUI's Frame property which is already in DIPs and properly positioned
        // This avoids all the coordinate conversion issues we've been having
        var frame = anchor.Frame;
        
        // The Frame is relative to the anchor's parent, but we need it relative to the screen/dialog
        // We need to walk up the visual tree to get the absolute position
        var absolutePosition = GetAbsolutePosition(anchor);
        
        return new Rect(absolutePosition.X, absolutePosition.Y, frame.Width, frame.Height);
    }

    private Point GetAbsolutePosition(IView view)
    {
        var x = view.Frame.X;
        var y = view.Frame.Y;
        
        // Walk up the visual tree to calculate absolute position
        var parent = view.Parent;
        while (parent is IView parentView)
        {
            x += parentView.Frame.X;
            y += parentView.Frame.Y;
            parent = parentView.Parent;
        }
        
        return new Point(x, y);
    }

    public Rect GetContainerBounds(IMauiContext mauiContext, bool ignoreSafeArea)
    {
        // Use platform services to get proper screen bounds
        var windowManager = context.GetSystemService(Context.WindowService) as IWindowManager;
        if (windowManager == null)
            return new Rect(0, 0, 1000, 1000);

        // Get screen size in density-independent pixels (DIPs)
        Size screenSize;
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var windowMetrics = windowManager.CurrentWindowMetrics;
            var density = context.Resources?.DisplayMetrics?.Density ?? 1.0f;
            screenSize = new Size(
                windowMetrics.Bounds.Width() / density,
                windowMetrics.Bounds.Height() / density);
        }
        else if (windowManager.DefaultDisplay != null)
        {
            var realSize = new Android.Graphics.Point();
            windowManager.DefaultDisplay.GetRealSize(realSize);
            var density = context.Resources?.DisplayMetrics?.Density ?? 1.0f;
            screenSize = new Size(realSize.X / density, realSize.Y / density);
        }
        else
        {
            screenSize = new Size(1000, 1000);
        }

        return new Rect(0, 0, screenSize.Width, screenSize.Height);
    }

    public Microsoft.Maui.Thickness GetSafeAreaInsets(IMauiContext mauiContext)
    {
        if (context.Resources == null)
            return new Microsoft.Maui.Thickness();

        var density = context.Resources.DisplayMetrics?.Density ?? 1.0f;

        // Get status bar height in DIPs
        var statusBarHeight = 0.0;
        int statusBarResourceId = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
        if (statusBarResourceId > 0)
        {
            var pixelSize = context.Resources.GetDimensionPixelSize(statusBarResourceId);
            statusBarHeight = pixelSize / density;
        }

        // Get navigation bar height in DIPs
        var navigationBarHeight = 0.0;
        var windowManager = context.GetSystemService(Context.WindowService) as IWindowManager;
        if (windowManager != null)
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

            navigationBarHeight = heightPixels / density;
        }

        return new Microsoft.Maui.Thickness(0, statusBarHeight, 0, navigationBarHeight);
    }
}
#endif