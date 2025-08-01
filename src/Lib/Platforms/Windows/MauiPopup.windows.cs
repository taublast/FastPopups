using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Platform;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;

namespace AppoMobi.Maui.Popups;

#if WINDOWS
/// <summary>
/// The native implementation of Popup control.
/// </summary>
public partial class MauiPopup : Microsoft.UI.Xaml.Controls.Grid
{
	readonly IMauiContext mauiContext;
	bool attached;
	Grid? overlay;
	bool windowResizeHandlerAttached;

	/// <summary>
	/// The native popup view.
	/// </summary>
	public Microsoft.UI.Xaml.Controls.Primitives.Popup PopupView { get; protected set; }

	/// <summary>
	/// The native fullscreen overlay
	/// </summary>
	public Grid? Overlay 
	{
		get
		{
			return overlay;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public bool CanBeDismissedByTappingOutside { get; set; }


	partial class BackgroundDimmer : Microsoft.UI.Xaml.Controls.Grid
	{
		public BackgroundDimmer(Action actionTapped)
		{
			PointerPressed += (s, e) =>
			{
				actionTapped?.Invoke();
			};
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="mauiContext"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public MauiPopup(IMauiContext mauiContext)
	{

		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));

		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		Background = null; // Ensure MauiPopup itself has no background blocking transparency

		PopupView = new ()
		{
			LightDismissOverlayMode = LightDismissOverlayMode.Off,
			IsLightDismissEnabled = false
		};

		// Don't add PopupView to Children - we'll manage it differently
		// Children.Add(PopupView);
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public FrameworkElement? SetElement(IPopup? element)
	{
		if (element == null)
		{
			PopupView.IsOpen = false;
			PopupView.Closed -= OnClosed;

			try
			{
				var window = mauiContext.GetPlatformWindow();
				if (window.Content is Panel rootPanel)
				{
					rootPanel.Children.Remove(this);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine(e);
			}

			VirtualView = null;

			if (Content is not null)
			{
				Content.SizeChanged -= OnSizeChanged;
				Content = null;
			}
			
			// Detach window resize handler
			DetachWindowResizeHandler();

			return null;
		}

		VirtualView = element;

		if (TryCreateContent(VirtualView, out var mauiContent))
		{
			// NEW APPROACH: Create a composite popup content that includes both overlay and content
			var compositeContent = CreateCompositePopupContent(mauiContent);

			PopupView.Child = compositeContent;
			Content = mauiContent; // Keep reference to the actual content
			mauiContent.SizeChanged += OnSizeChanged;
			PopupView.Closed += OnClosed;
		}

		return mauiContent;
	}

	/// <summary>
	/// Creates a composite popup content that includes both the full-screen overlay and the actual popup content.
	/// This ensures the overlay is part of the WinUI Popup and can darken existing popups.
	/// </summary>
	/// <param name="actualContent">The actual popup content.</param>
	/// <returns>A Grid containing both overlay and content.</returns>
	FrameworkElement CreateCompositePopupContent(FrameworkElement actualContent)
	{
		// Create a full-screen container with no background to avoid transparency issues
		var container = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = null // Explicitly null to avoid any white background blocking transparency
		};

		if (VirtualView == null)
		{
			return container;
		}


		// Create the full-screen overlay with proper transparency handling
		var overlayColor = ((Popup)VirtualView).BackgroundColor;
		overlay = new BackgroundDimmer(() =>
		{
			if (CanBeDismissedByTappingOutside)
			{
				VirtualView?.OnDismissedByTappingOutsideOfPopup();
			}
		})
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		// Set background with proper transparency - avoid any white blocking issues
		if (overlayColor != Colors.Transparent)
		{
			var windowsColor = overlayColor.ToWindowsColor();
			overlay.Background = new SolidColorBrush(windowsColor);
		}
		else
		{
			overlay.Background = null; // Explicitly null for transparent overlays
		}

		// Add overlay first (behind content)
		container.Children.Add(overlay);

		// Position the actual content within the fullscreen container
		PositionContentInContainer(container, actualContent);

		return container;
	}

	/// <summary>
	/// Positions the actual popup content within the fullscreen container using layout options.
	/// </summary>
	/// <param name="container">The fullscreen container Grid.</param>
	/// <param name="actualContent">The popup content to position.</param>
	void PositionContentInContainer(Grid container, FrameworkElement actualContent)
	{
		if (VirtualView == null) return;

		// Get window bounds for layout calculation
		var window = mauiContext.GetPlatformWindow();
		var windowBounds = window.Bounds;
		var parentBounds = new Rect(0, 0, windowBounds.Width, windowBounds.Height);

		// Get content size
		var contentSize = new Size(actualContent.Width, actualContent.Height);
		if (double.IsNaN(contentSize.Width) || double.IsNaN(contentSize.Height))
		{
			actualContent.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
			contentSize = new Size(actualContent.DesiredSize.Width, actualContent.DesiredSize.Height);
		}

		// Use unified layout calculator for positioning
		var (x, y) = PopupLayoutCalculator.CalculatePosition(VirtualView, contentSize, parentBounds);

		// Set positioning using margins
		actualContent.HorizontalAlignment = HorizontalAlignment.Left;
		actualContent.VerticalAlignment = VerticalAlignment.Top;
		actualContent.Margin = new Microsoft.UI.Xaml.Thickness(x, y, 0, 0);

		// Add content to container
		container.Children.Add(actualContent);
	}

	/// <summary>
	/// Opens the popup and shows the dimmer.
	/// </summary>
	public void Show()
	{
		if (!attached)
		{
			var window = mauiContext.GetPlatformWindow();
			if (window.Content is Panel rootPanel)
			{
				attached = true;
				// Simple approach: just add to the end of the root panel
				// The overlay is now part of the popup content, so it will automatically
				// cover everything that was visible before this popup opened
				rootPanel.Children.Add(this);
			}
			
			// Attach window resize handler to maintain popup positioning
			AttachWindowResizeHandler();
		}

		PopupView.XamlRoot = this.XamlRoot;
		PopupView.IsOpen = true;

		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");
		
		// Delay layout until after the popup is shown and content is measured
		// This fixes the initial positioning issue
		DispatcherQueue.TryEnqueue(() =>
		{
			Layout(); // Re-layout after popup is fully shown
		});
		
		VirtualView.OnOpened();
	}



	void OnClosed(object? sender, object e)
	{
		if (!PopupView.IsOpen && this.CanBeDismissedByTappingOutside && VirtualView is not null)
		{
			VirtualView.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public void OnSizeChanged(object? sender, object e)
	{
		UpdateLayout();
	}

	/// <summary>
	/// Measure and layout popup
	/// </summary>
	public void Layout()
	{
		if (VirtualView is not null)
		{
			PopupExtensions.SetSize(this, VirtualView, mauiContext);
			PopupExtensions.SetLayout(this, VirtualView, mauiContext);
		}
	}		

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; protected set; }

	/// <summary>
	/// 
	/// </summary>
	public FrameworkElement? Content { get; protected set; }

	bool TryCreateContent(in IPopup popup, [NotNullWhen(true)] out FrameworkElement? container)
	{
		container = null;

		if (popup.Content is null)
		{
			return false;
		}

		container = popup.Content.ToPlatform(mauiContext);
		//Children.Add(container);

		return true;
	}

	/// <summary>
	/// Attaches window resize handler to maintain popup positioning when window is resized.
	/// </summary>
	void AttachWindowResizeHandler()
	{
		if (windowResizeHandlerAttached)
			return;

		try
		{
			var window = mauiContext.GetPlatformWindow();
			window.SizeChanged += OnWindowSizeChanged;
			windowResizeHandlerAttached = true;
		}
		catch (Exception e)
		{
			Trace.WriteLine($"Failed to attach window resize handler: {e}");
		}
	}

	/// <summary>
	/// Detaches window resize handler.
	/// </summary>
	void DetachWindowResizeHandler()
	{
		if (!windowResizeHandlerAttached)
			return;

		try
		{
			var window = mauiContext.GetPlatformWindow();
			window.SizeChanged -= OnWindowSizeChanged;
			windowResizeHandlerAttached = false;
		}
		catch (Exception e)
		{
			Trace.WriteLine($"Failed to detach window resize handler: {e}");
		}
	}

	/// <summary>
	/// Handles window resize events to maintain popup positioning.
	/// </summary>
	void OnWindowSizeChanged(object? sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs e)
	{
		if (VirtualView is not null && PopupView.IsOpen)
		{
			// Recalculate and update popup position
			Layout();
		}
	}


}

#endif