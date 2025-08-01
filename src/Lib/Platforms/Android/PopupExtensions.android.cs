using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;

//using AppoMobi.Maui.Popups;
using Microsoft.Maui.Platform;
using static Android.Views.ViewGroup;
using AColorRes = Android.Resource.Color;
using APoint = Android.Graphics.Point;
using AView = Android.Views.View;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;


namespace AppoMobi.Maui.Popups;

/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static partial class PopupExtensions
{
	/// <summary>
	/// Method to update the <see cref="IPopup.Anchor"/> view. With the new architecture,
	/// anchored positioning is handled by the content positioning within the fullscreen container.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	/// <exception cref="InvalidOperationException">if the <see cref="Window"/> is null an exception will be thrown.</exception>
	public static void SetAnchor(this Dialog dialog, in IPopup popup)
	{
		// With the new architecture, we don't position the dialog window itself.
		// Instead, the content positioning is handled in the CreateCompositePopupContent method
		// of MauiPopup, where the actual content is positioned within the fullscreen container.
		
		// This method is kept for compatibility but no longer needs to manipulate window positioning.
		// Anchor-specific positioning logic will be implemented in the MauiPopup content positioning system.
	}


	/// <summary>
	/// Method to update the popup background color.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetBackgroundColor(this MauiPopup dialog, in IPopup popup)
	{
		if (dialog.Overlay != null)
		{
			var color = ((Popup)popup).BackgroundColor ?? Colors.Transparent;
			dialog.Overlay.SetBackgroundColor(color.ToPlatform());
		}
	}

	/// <summary>
	/// Method to update the <see cref="IPopup.CloseWhenBackgroundIsClicked"/> property.
	/// </summary>
	/// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
	/// <param name="popup">An instance of <see cref="IPopup"/>.</param>
	public static void SetCloseWhenBackgroundIsClicked(this Dialog dialog, in IPopup popup)
	{
		dialog.SetCancelable(popup.CloseWhenBackgroundIsClicked);
		dialog.SetCanceledOnTouchOutside(popup.CloseWhenBackgroundIsClicked);
	}

    /// <summary>
    /// Method to update the <see cref="IPopup.Size"/> property.
    /// </summary>
    /// <param name="dialog">An instance of <see cref="Dialog"/>.</param>
    /// <param name="popup">An instance of <see cref="IPopup"/>.</param>
    /// <param name="container">The native representation of <see cref="IPopup.Content"/>.</param>
    /// <param name="handler">An instance of <see cref="AppoMobi.Maui.Popups.PopupHandler"/>.</param>
    /// <exception cref="InvalidOperationException">if the <see cref="Window"/> is null an exception will be thrown. If the <paramref name="container"/> is null an exception will be thrown.</exception>
    public static void SetSize(this Dialog dialog, in IPopup popup, in AView container, AppoMobi.Maui.Popups.PopupHandler handler)
	{
		ArgumentNullException.ThrowIfNull(dialog);
		ArgumentNullException.ThrowIfNull(container);
		ArgumentNullException.ThrowIfNull(popup.Content);
		ArgumentNullException.ThrowIfNull(handler);

		var window = GetWindow(dialog);
		var decorView = (ViewGroup)window.DecorView;

		if (dialog.Window != null)
		{
			// Remove any padding from the decorView
			decorView.SetPadding(0, 0, 0, 0);

			// Remove padding and margins from child views
			for (int i = 0; i < decorView.ChildCount; i++)
			{
				var child = decorView.GetChildAt(i);
				child?.SetPadding(0, 0, 0, 0);
				if (child?.LayoutParameters is ViewGroup.MarginLayoutParams marginParams)
				{
					marginParams.SetMargins(0, 0, 0, 0);
					child.LayoutParameters = marginParams;
				}
			}
		}

		// Dialog window should always be fullscreen so overlay fills entire screen
		window.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

		SetAnchor(dialog, popup);
	}


	static Android.Views.Window GetWindow(in Dialog dialog) =>
		dialog.Window ?? throw new InvalidOperationException($"{nameof(Dialog)}.{nameof(Dialog.Window)} cannot be null");

	static Size GetWindowSize(Context context, [NotNull] IWindowManager? windowManager, bool isFullscreen)
	{
		ArgumentNullException.ThrowIfNull(windowManager);

		int windowWidth;
		int windowHeight;
		int statusBarHeight;
		int navigationBarHeight;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			windowWidth = windowMetrics.Bounds.Width();
			windowHeight = windowMetrics.Bounds.Height();
			statusBarHeight = windowInsets.Top;
			navigationBarHeight = windowHeight < windowWidth ? windowInsets.Left + windowInsets.Right : windowInsets.Bottom;
		}
		else if (windowManager.DefaultDisplay is null)
		{
			throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
		}
		else
		{
			APoint realSize = new();
			APoint displaySize = new();
			APoint displaySmallSize = new();
			APoint displayLargeSize = new();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			ArgumentNullException.ThrowIfNull(realSize);

			windowManager.DefaultDisplay.GetSize(displaySize);
			ArgumentNullException.ThrowIfNull(displaySize);

			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);
			ArgumentNullException.ThrowIfNull(displaySmallSize);
			ArgumentNullException.ThrowIfNull(displayLargeSize);

			windowWidth = realSize.X;
			windowHeight = realSize.Y;

			if (displaySize.X > displaySize.Y)
			{
				statusBarHeight = displaySize.Y - displaySmallSize.Y;
			}
			else
			{
				statusBarHeight = displaySize.Y - displayLargeSize.Y;
			}

			navigationBarHeight = realSize.Y < realSize.X
									? (realSize.X - displaySize.X)
									: (realSize.Y - displaySize.Y);
		}

		if (isFullscreen)
		{
			//navigationBarHeight = 0;
			statusBarHeight = -GetStatusBarHeight(context);
		}

		windowWidth -= windowHeight < windowWidth
			? navigationBarHeight
			: 0;

		windowHeight -= (windowHeight < windowWidth
			? 0
			: navigationBarHeight)
			+ statusBarHeight;

		return new Size(windowWidth, windowHeight);
	}

	static int GetNavigationBarHeight(IWindowManager? windowManager)
	{
		ArgumentNullException.ThrowIfNull(windowManager);

		int navigationBarHeight;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			var windowWidth = windowMetrics.Bounds.Width();
			var windowHeight = windowMetrics.Bounds.Height();
			navigationBarHeight = windowHeight < windowWidth ? windowInsets.Left + windowInsets.Right : windowInsets.Bottom;
		}
		else if (windowManager.DefaultDisplay is null)
		{
			throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
		}
		else
		{
			APoint realSize = new();
			APoint displaySize = new();
			APoint displaySmallSize = new();
			APoint displayLargeSize = new();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			ArgumentNullException.ThrowIfNull(realSize);

			windowManager.DefaultDisplay.GetSize(displaySize);
			ArgumentNullException.ThrowIfNull(displaySize);

			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);
			ArgumentNullException.ThrowIfNull(displaySmallSize);
			ArgumentNullException.ThrowIfNull(displayLargeSize);

			navigationBarHeight = realSize.Y < realSize.X
									? (realSize.X - displaySize.X)
									: (realSize.Y - displaySize.Y);
		}

		return navigationBarHeight;
	}

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

	static int GetStatusBarHeight(IWindowManager? windowManager)
	{
		ArgumentNullException.ThrowIfNull(windowManager);

		int statusBarHeight;

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var windowMetrics = windowManager.CurrentWindowMetrics;
			var windowInsets = windowMetrics.WindowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
			statusBarHeight = windowInsets.Top;
		}
		else if (windowManager.DefaultDisplay is null)
		{
			throw new InvalidOperationException($"{nameof(IWindowManager)}.{nameof(IWindowManager.DefaultDisplay)} cannot be null");
		}
		else
		{
			APoint realSize = new();
			APoint displaySize = new();
			APoint displaySmallSize = new();
			APoint displayLargeSize = new();

			windowManager.DefaultDisplay.GetRealSize(realSize);
			ArgumentNullException.ThrowIfNull(realSize);

			windowManager.DefaultDisplay.GetSize(displaySize);
			ArgumentNullException.ThrowIfNull(displaySize);

			windowManager.DefaultDisplay.GetCurrentSizeRange(displaySmallSize, displayLargeSize);
			ArgumentNullException.ThrowIfNull(displaySmallSize);
			ArgumentNullException.ThrowIfNull(displayLargeSize);

			if (displaySize.X > displaySize.Y)
			{
				statusBarHeight = displaySize.Y - displaySmallSize.Y;
			}
			else
			{
				statusBarHeight = displaySize.Y - displayLargeSize.Y;
			}
		}

		return statusBarHeight;
	}

    public static Android.App.Activity GetPlatformWindow(this IMauiContext mauiContext) =>
        mauiContext.Services.GetRequiredService<Android.App.Activity>();

    internal static Android.Views.View ToPlatform(this IElement view)
    {
        if (view is IReplaceableView replaceableView && replaceableView.ReplacedView != view)
            return replaceableView.ReplacedView.ToPlatform();


        _ = view.Handler ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set on parent.");

        if (view.Handler is IViewHandler viewHandler)
        {
            if (viewHandler.ContainerView is Android.Views.View containerView)
                return containerView;

            if (viewHandler.PlatformView is Android.Views.View platformView)
                return platformView;
        }

        return (view.Handler?.PlatformView as Android.Views.View) ?? throw new InvalidOperationException($"Unable to convert {view} to {typeof(Android.Views.View)}");

    }

}