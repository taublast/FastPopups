using System.Diagnostics;
using FastPopups;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using System.Runtime.CompilerServices;
using Page = Microsoft.Maui.Controls.Page;

namespace FastPopups;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
    #region DI

    /// <summary>
    /// Configures the <see cref="MauiAppBuilder"/> to register popup handlers and services.
    /// </summary>
    /// <param name="builder">The <see cref="MauiAppBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="MauiAppBuilder"/>.</returns>
    public static MauiAppBuilder AddPopups(this MauiAppBuilder builder)
    {
        // Register the popup handler
        builder.ConfigureMauiHandlers(handlers => { handlers.AddHandler<IPopup, PopupHandler>(); });

        return builder;
    }

    #endregion

#if WINDOWS || ANDROID || TIZEN
internal static IWindow GetWindow(this IElement element) =>
        element.Handler?.MauiContext?.GetPlatformWindow()?.GetWindow() ??
        throw new InvalidOperationException("IWindow not found");

#endif

	/// <summary>
	/// Recursively navigates the MAUI page hierarchy to find the currently visible page.
	/// </summary>
	/// <param name="currentPage">The page to start searching from, typically the application's main page.</param>
	/// <returns>The currently visible <see cref="Page"/> in the navigation hierarchy.</returns>
	/// <remarks>
	/// <para>
	/// This method intelligently traverses different MAUI navigation patterns to find the actual visible page:
	/// </para>
	/// <list type="number">
	/// <item><description>Checks for modal pages (highest priority)</description></item>
	/// <item><description>Navigates into <see cref="FlyoutPage"/> detail pages</description></item>
	/// <item><description>Resolves <see cref="Shell"/> presented pages</description></item>
	/// <item><description>Traverses <see cref="IPageContainer{T}"/> containers (TabbedPage, NavigationPage, etc.)</description></item>
	/// <item><description>Returns the page itself if no container wrapping is found</description></item>
	/// </list>
	/// <para>
	/// This is used internally to determine where to attach popups in the visual tree.
	/// </para>
	/// </remarks>
	public static Page GetCurrentPage(this Page currentPage)
    {
        if (currentPage.NavigationProxy.ModalStack.LastOrDefault() is Page modal)
            return modal;
        else if (currentPage is FlyoutPage fp)
            return GetCurrentPage(fp.Detail);
        else if (currentPage is Shell shell && shell.CurrentItem?.CurrentItem is IShellSectionController ssc)
            return ssc.PresentedPage;
        else if (currentPage is IPageContainer<Page> pc)
            return GetCurrentPage(pc.CurrentPage);
        else
            return currentPage;
    }

    /// <summary>
    /// Displays a popup.
    /// </summary>
    /// <param name="page">The current <see cref="Page"/>.</param>
    /// <param name="popup">The <see cref="Popup"/> to display.</param>
    public static void ShowPopup<TPopup>(this Page page, TPopup popup) where TPopup : Popup
    {
#if WINDOWS
		// TODO: This is a workaround for https://github.com/dotnet/maui/issues/12970. Remove this `#if Windows` block when the issue is closed
		page.GetCurrentPage().Loaded += handler;

		void handler(object? sender, EventArgs args)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var page = (Page)sender;

			page.GetCurrentPage().Loaded -= handler;

			CreateAndShowPopup(page, popup);
		}
#else
        if (page.IsPlatformEnabled)
        {
            CreateAndShowPopup(page, popup);
        }
        else
        {
            void handler(object? sender, EventArgs args)
            {
                page.Loaded -= handler;

                CreateAndShowPopup(page, popup);
            }

            page.Loaded += handler;
        }
#endif
    }

    /// <summary>
    /// Displays a popup and returns a result.
    /// </summary>
    /// <param name="page">The current <see cref="Page"/>.</param>
    /// <param name="popup">The <see cref="Popup"/> to display.</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    /// <returns>
    /// A task that will complete once the <see cref="Popup"/> is dismissed.
    /// </returns>
    public static Task<object?> ShowPopupAsync<TPopup>(this Page page, TPopup popup, CancellationToken token = default)
        where TPopup : Popup
    {
#if WINDOWS
		// TODO: This is a workaround for https://github.com/dotnet/maui/issues/12970. Remove this `#if Windows` block when the issue is closed   
		var taskCompletionSource = new TaskCompletionSource<object?>();

		page.GetCurrentPage().Unloaded += unloadedHandler;
		page.GetCurrentPage().Loaded += loadedHandler;

		return taskCompletionSource.Task.WaitAsync(token);

		static void unloadedHandler(object? sender, EventArgs args) { }

		async void loadedHandler(object? sender, EventArgs args)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var page = (Page)sender;

			page.Unloaded -= unloadedHandler;
			page.Loaded -= loadedHandler;

			try
			{
				var result = await CreateAndShowPopupAsync(page, popup, token);

				taskCompletionSource.TrySetResult(result);
			}
			catch (Exception ex)
			{
				taskCompletionSource.TrySetException(ex);
			}
		}
#else
        if (page.IsPlatformEnabled)
        {
            return CreateAndShowPopupAsync(page, popup, token);
        }
        else
        {
            var taskCompletionSource = new TaskCompletionSource<object?>();

            async void handler(object? sender, EventArgs args)
            {
                page.Loaded -= handler;

                try
                {
                    var result = await CreateAndShowPopupAsync(page, popup, token);

                    taskCompletionSource.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            }

            page.Loaded += handler;

            return taskCompletionSource.Task;
        }
#endif
    }

    static void CreatePopup(Page page, Popup popup)
    {
        var mauiContext = GetMauiContext(page);

        var parent = page.GetCurrentPage();
        parent?.AddLogicalChild(popup);

        // Add to navigation stack
        PopupNavigationStack.Instance.Push(popup);

        try
        {
            var platformPopup = popup.ToHandler(mauiContext);

            platformPopup.Invoke(nameof(IPopup.OnOpened));
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static IMauiContext GetMauiContext(Page page)
    {
        return page.Handler?.MauiContext ?? throw new InvalidOperationException("Could not locate MauiContext.");
    }

    static void CreateAndShowPopup<TPopup>(Page page, TPopup popup) where TPopup : Popup
    {
        CreatePopup(page, popup);
    }

    static Task<object?> CreateAndShowPopupAsync<TPopup>(this Page page, TPopup popup, CancellationToken token)
        where TPopup : Popup
    {
        CreatePopup(page, popup);

        return popup.Result.WaitAsync(token);
    }

}