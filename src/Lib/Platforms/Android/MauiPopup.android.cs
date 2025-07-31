using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
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
	/// </summary>
	/// <param name="actualContent">The actual popup content.</param>
	/// <returns>A FrameLayout containing both overlay and content.</returns>
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
		var color = VirtualView.OverlayColor ?? Colors.Transparent;
		overlay.SetBackgroundColor(color.ToPlatform());

		// Add overlay first (behind content)
		container.AddView(overlay);

		// Add actual content on top of overlay
		container.AddView(actualContent);

		return container;
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
			if (VirtualView.CanBeDismissedByTappingOutsideOfPopup &&
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