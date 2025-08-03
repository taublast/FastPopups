using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Application = Microsoft.Maui.Controls.Application;
using Page = Microsoft.Maui.Controls.Page;

namespace AppoMobi.Maui.Popups;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{

	static readonly Dictionary<Type, Type> viewModelToViewMappings = [];

	readonly IServiceProvider serviceProvider;
	readonly IDispatcher dispatcher;

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementation.</param>
	/// <param name="dispatcherProvider"></param>
	[ActivatorUtilitiesConstructor]
	public PopupService(IServiceProvider serviceProvider, IDispatcherProvider dispatcherProvider)
	{
		this.serviceProvider = serviceProvider;
		dispatcher = dispatcherProvider.GetForCurrentThread()
						?? throw new InvalidOperationException("Could not locate IDispatcher");
	}

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	public PopupService()
	{
		serviceProvider = Application.Current?.Handler?.MauiContext?.Services
			?? throw new InvalidOperationException("Could not locate IServiceProvider");

		dispatcher = Application.Current.Dispatcher
			?? throw new InvalidOperationException("Could not locate IDispatcher");
	}

	/// <summary>
	/// Gets or sets the <see cref="IPopupLifecycleController"/> implementation.
	/// </summary>
	public IPopupLifecycleController PopupLifecycleController { get; set; } = new PopupLifecycleController();

	static Page CurrentPage =>
        PopupExtensions.GetCurrentPage(
			Application.Current?.Windows[0].Page ?? throw new InvalidOperationException("Application.Current?.Windows[0].Page cannot be null."));

	internal static void ClearViewModelToViewMappings() => viewModelToViewMappings.Clear();

	internal static void AddTransientPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(IServiceCollection services)
		where TPopupView : IPopup
		where TPopupViewModel : INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient(typeof(TPopupView));
		services.AddTransient(typeof(TPopupViewModel));
	}

	internal static void AddTransientPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(TPopupView popup, TPopupViewModel popupViewModel, IServiceCollection services)
		where TPopupView : class, IPopup
		where TPopupViewModel : class, INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient<TPopupView>(_ => popup);
		services.AddTransient<TPopupViewModel>(_ => popupViewModel);
	}

	/// <inheritdoc cref="IPopupService.ClosePopup(object?)" />
	public void ClosePopup(object? result = null)
	{
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PopupLifecycleController.GetCurrentPopup()?.Close(result);
        });
	}

	/// <inheritdoc cref="IPopupService.ClosePopupAsync(object?)" />
	public Task ClosePopupAsync(object? result = null)
	{
		var popup = PopupLifecycleController.GetCurrentPopup();

		return popup?.CloseAsync(result) ?? Task.CompletedTask;
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}()"/>
	public void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged
	{
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var popup = GetPopup(typeof(TViewModel));
            ValidateBindingContext<TViewModel>(popup, out _);

            // Add to navigation stack and hook close event
            PopupNavigationStack.Instance.Push(popup);
            HookPopupCloseEvent(popup);

            InitializePopup(popup);
            ShowPopup(popup);
        });
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(Action{TViewModel})"/>
	public void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		ValidateBindingContext(popup, out TViewModel viewModel);

		onPresenting.Invoke(viewModel);

		// Add to navigation stack and hook close event
		PopupNavigationStack.Instance.Push(popup);
		HookPopupCloseEvent(popup);

		InitializePopup(popup);

		ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(CancellationToken)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(CancellationToken token = default) where TViewModel : INotifyPropertyChanged
	{
		var popup = GetPopup(typeof(TViewModel));

		ValidateBindingContext<TViewModel>(popup, out _);

		// Add to navigation stack and hook close event
		PopupNavigationStack.Instance.Push(popup);
		HookPopupCloseEvent(popup);

		InitializePopup(popup);

		return ShowPopupAsync(popup, token);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(Action{TViewModel}, CancellationToken)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting, CancellationToken token = default) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		ValidateBindingContext(popup, out TViewModel viewModel);

		onPresenting.Invoke(viewModel);

		// Add to navigation stack and hook close event
		PopupNavigationStack.Instance.Push(popup);
		HookPopupCloseEvent(popup);

		InitializePopup(popup);

		return ShowPopupAsync(popup, token);
	}

	static Task<object?> ShowPopupAsync(Popup popup, CancellationToken token)
	{
#if WINDOWS
		if (Application.Current is Microsoft.Maui.Controls.Application app)
		{
            //todo investigate
            if (app.Windows.LastOrDefault() is Microsoft.Maui.Controls.Window activeWindow)
            //if (app.Windows.FirstOrDefault(x => x.IsActivated) is Microsoft.Maui.Controls.Window activeWindow)
            {
				if (activeWindow.Page is Page page)
				{
					return page.ShowPopupAsync(popup, token);
				}
			}
		}
		return CurrentPage.ShowPopupAsync(popup, token);
#else
		return CurrentPage.ShowPopupAsync(popup, token);
#endif
	}

	/// <summary>
	/// Ensures that the BindingContext property of the Popup to present is properly assigned and of the expected type.
	/// </summary>
	/// <typeparam name="TViewModel"></typeparam>
	/// <param name="popup">The popup to be presented.</param>
	/// <param name="bindingContext">Validated View Model</param>
	/// <exception cref="InvalidOperationException"></exception>
	static void ValidateBindingContext<TViewModel>(Popup popup, out TViewModel bindingContext)
	{
		if (popup.BindingContext is not TViewModel viewModel)
		{
			throw new InvalidOperationException($"Unexpected type has been assigned to the BindingContext of {popup.GetType().FullName}. Expected type {typeof(TViewModel).FullName} but was {popup.BindingContext?.GetType().FullName ?? "null"}");
		}

		bindingContext = viewModel;
	}

	static void ShowPopup(Popup popup)
	{
#if WINDOWS
		if (Application.Current is Application app)
		{
            if (app.Windows.LastOrDefault() is Microsoft.Maui.Controls.Window activeWindow)
            //if (app.Windows.FirstOrDefault(x => x.IsActivated) is Window activeWindow)
			{
				if (activeWindow.Page is Page page)
				{
					page.ShowPopup(popup);
					return;
				}
			}
		}
		CurrentPage.ShowPopup(popup);
#else
		CurrentPage.ShowPopup(popup);
#endif
	}


	Popup GetPopup(Type viewModelType)
	{
		var popup = (Popup)(serviceProvider.GetService(viewModelToViewMappings[viewModelType])
			?? throw new InvalidOperationException($"Unable to resolve popup type for {viewModelType} please make sure that you have called {nameof(PopupService)}.{nameof(AddTransientPopup)} in MauiProgram.cs"));
		return popup;
	}

	void InitializePopup(Popup popup)
	{
		PopupLifecycleController.OnShowPopup(popup);
	}

	// Add stack-based methods
	public void CloseTopPopup(object? result = null)
	{
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var popup = PopupNavigationStack.Instance.Pop();
            popup?.Close(result);
        });
	}

	public void CloseAllPopups(object? result = null)
	{
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PopupNavigationStack.Instance.Clear();
        });
	}

	/// <summary>
	/// Hooks into the popup's Closed event to automatically remove it from the navigation stack
	/// </summary>
	/// <param name="popup">The popup to hook</param>
	private static void HookPopupCloseEvent(Popup popup)
	{
		popup.Closed += OnPopupClosed;
	}

	/// <summary>
	/// Event handler for when a popup closes - removes it from the navigation stack
	/// </summary>
	private static void OnPopupClosed(object? sender, PopupClosedEventArgs e)
	{
		if (sender is Popup popup)
		{
			// Remove the event handler to prevent memory leaks
			popup.Closed -= OnPopupClosed;

			// Remove the popup from the navigation stack
			PopupNavigationStack.Instance.Remove(popup);
		}
	}
}
