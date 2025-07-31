using Android.Views;
using System.Diagnostics.CodeAnalysis;

namespace AppoMobi.Maui.Popups;

static class JavaObjectExtensions
{
    const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors;

    public static bool IsDisposed(this Java.Lang.Object obj)
    {
        return obj.Handle == IntPtr.Zero;
    }

    public static bool IsDisposed(this global::Android.Runtime.IJavaObject obj)
    {
        return obj.Handle == IntPtr.Zero;
    }

    public static bool IsAlive([NotNullWhen(true)] this Java.Lang.Object? obj)
    {
        if (obj == null)
            return false;

        return !obj.IsDisposed();
    }

    public static bool IsAlive([NotNullWhen(true)] this global::Android.Runtime.IJavaObject? obj)
    {
        if (obj == null)
            return false;

        return !obj.IsDisposed();
    }
}

/// <summary>
/// Provides extension methods for the Window class.
/// </summary>
public static class AndroidWindowExtensions
{
 
    /// <summary>
    /// Gets the current window associated with the specified activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The current window.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity window is null.</exception>
    public static Android.Views.Window GetCurrentWindow(this Activity activity)
	{
		var window = activity.Window ?? throw new InvalidOperationException($"{nameof(activity.Window)} cannot be null");
		window.ClearFlags(WindowManagerFlags.TranslucentStatus);
		window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
		return window;
	}
}