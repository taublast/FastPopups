using Android.Content;
using Android.Content.Res;
using Android.Views;
using AView = Android.Views.View;

namespace AppoMobi.Maui.Popups;

#if ANDROID
/// <summary>
/// A View wrapper that manages a MauiPopup Dialog.
/// This allows us to use ViewHandler while still using Dialog for popup functionality.
/// </summary>
public class MauiPopupView : AView
{
	MauiPopup? dialog;

    protected override void OnConfigurationChanged(Configuration? newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        if (VirtualView != null && Visibility == ViewStates.Visible)
        {
            // todo
            //dialog?.RecalculatePosition();
        }
    }

    /// <summary>
	/// Constructor for MauiPopupView.
	/// </summary>
	/// <param name="context">Android Context.</param>
	public MauiPopupView(Context context) : base(context)
	{
		// Make this view invisible - it's just a container for the dialog
		Visibility = ViewStates.Gone;
	}

	/// <summary>
	/// Gets the internal dialog.
	/// </summary>
	public MauiPopup? Dialog => dialog;

	/// <summary>
	/// Creates and configures the internal dialog.
	/// </summary>
	/// <param name="context">Android Context.</param>
	/// <param name="mauiContext">MAUI Context.</param>
	public void CreateDialog(Context context, Microsoft.Maui.IMauiContext mauiContext, bool ignoreSafeArea)
	{
		dialog?.Dispose();
		dialog = new MauiPopup(context, mauiContext, ignoreSafeArea);
	}

	/// <summary>
	/// Sets the popup element on the internal dialog.
	/// </summary>
	/// <param name="popup">The popup element.</param>
	/// <returns>The content view.</returns>
	public AView? SetElement(IPopup popup)
	{
		return dialog?.SetElement(popup);
	}

	/// <summary>
	/// Shows the internal dialog.
	/// </summary>
	public void ShowDialog()
	{
		dialog?.Show();
	}

	/// <summary>
	/// Dismisses the internal dialog.
	/// </summary>
	public void DismissDialog()
	{
		dialog?.Dismiss();
	}

	/// <summary>
	/// Sets fullscreen mode on the internal dialog.
	/// </summary>
	/// <param name="fullscreen">Whether to use fullscreen mode.</param>
	public void SetFullScreen(bool fullscreen)
	{
		dialog?.SetFullScreen(fullscreen);
	}

	/// <summary>
	/// Gets the virtual view from the internal dialog.
	/// </summary>
	public IPopup? VirtualView => dialog?.VirtualView;

	/// <summary>
	/// Gets the overlay from the internal dialog.
	/// </summary>
	public new AView? Overlay => dialog?.Overlay;

	/// <summary>
	/// Checks if the internal dialog is disposed.
	/// </summary>
	/// <returns>True if disposed.</returns>
	public bool IsDisposed()
	{
		return dialog?.IsDisposed() ?? true;
	}

	/// <summary>
	/// Checks if the internal dialog is showing.
	/// </summary>
	public bool IsShowing => dialog?.IsShowing ?? false;

	/// <summary>
	/// Clean up resources.
	/// </summary>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			dialog?.CleanUp();
			dialog?.Dispose();
			dialog = null;
		}
		base.Dispose(disposing);
	}
}

#endif