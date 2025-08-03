using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

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
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the popup.
	/// </summary>
	public bool CloseWhenBackgroundIsClicked { get; set; }

    public bool IgnoreSafeArea { get; set; }

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
			if (VirtualView is Popup popup && popup.ShouldDismissOnOutsideClick())
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
	/// Calculates the layout (size and position) for popup content.
	/// </summary>
	/// <param name="actualContent">The actual popup content to calculate layout for.</param>
	/// <returns>The calculated size and position.</returns>
	(Size ContentSize, double X, double Y) CalculateContentLayout(FrameworkElement actualContent)
	{
		if (VirtualView is null || mauiContext is null)
			return (new Size(300, 200), 0, 0);

		// Get window bounds for layout calculation
		var window = mauiContext.GetPlatformWindow();
		var windowBounds = window.Bounds;
		var parentBounds = new Rect(0, 0, windowBounds.Width, windowBounds.Height);

		// Apply safe area adjustments if needed
		var popupParentFrame = parentBounds;
		Microsoft.Maui.Thickness safeAreaInsets = default;
		if (!IgnoreSafeArea)
		{
			var safeArea = PopupExtensions.GetSafeArea(mauiContext);
			popupParentFrame = new Rect(safeArea.Left, safeArea.Y, safeArea.Width, safeArea.Height);
			safeAreaInsets = new Microsoft.Maui.Thickness(
				safeArea.X - parentBounds.X,
				safeArea.Y - parentBounds.Y,
				parentBounds.Right - safeArea.Right,
				parentBounds.Bottom - safeArea.Bottom);
		}

		// For Fill layouts, we need to calculate size based on layout options, not measured content
		// Check if we have Fill layout options first
		var horizontalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.HorizontalOptions);
		var verticalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.VerticalOptions);
		var isFillWidth = horizontalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
		var isFillHeight = verticalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

		Size contentSize;
		if (isFillWidth || isFillHeight)
		{
			// For Fill layouts, use the layout calculator to get proper Fill sizing
			contentSize = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, safeAreaInsets);
		}
		else
		{
			// For non-Fill layouts, try to get actual/measured content size first
			contentSize = new Size(actualContent.ActualWidth, actualContent.ActualHeight);
			if (contentSize.Width == 0 || contentSize.Height == 0)
			{
				if (actualContent.DesiredSize.Width == 0 || actualContent.DesiredSize.Height == 0)
				{
					actualContent.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
				}
				contentSize = new Size(actualContent.DesiredSize.Width, actualContent.DesiredSize.Height);
			}

			// If still no size, use layout calculator as fallback
			if (contentSize.Width == 0 || contentSize.Height == 0)
			{
				contentSize = PopupLayoutCalculator.CalculateContentSize(VirtualView, parentBounds, safeAreaInsets);
			}
		}

		// Calculate position based on popup alignment or anchor
		double x, y;
		if (VirtualView.Anchor != null)
		{
			// Handle anchored positioning
			var anchorBounds = PopupExtensions.GetAnchorBounds(VirtualView.Anchor, mauiContext);
			(x, y) = CrossPlatformAnchorCalculator.CalculateAnchoredPosition(VirtualView, contentSize, anchorBounds, parentBounds, safeAreaInsets);
		}
		else
		{
			// Handle regular alignment-based positioning
			(x, y) = PopupLayoutCalculator.CalculatePosition(VirtualView, contentSize, parentBounds, safeAreaInsets);
		}

		return (contentSize, x, y);
	}

	/// <summary>
	/// Positions the actual popup content within the fullscreen container using layout options.
	/// </summary>
	/// <param name="container">The fullscreen container Grid.</param>
	/// <param name="actualContent">The popup content to position.</param>
	void PositionContentInContainer(Grid container, FrameworkElement actualContent)
	{
		if (VirtualView == null) return;

		var (contentSize, x, y) = CalculateContentLayout(actualContent);

		// Apply the calculated size to the content for Fill layouts
		var horizontalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.HorizontalOptions);
		var verticalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.VerticalOptions);
		var isFillWidth = horizontalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
		var isFillHeight = verticalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

		if (isFillWidth)
		{
			actualContent.Width = contentSize.Width;
		}

		if (isFillHeight)
		{
			actualContent.Height = contentSize.Height;
		}

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
		if (!PopupView.IsOpen && VirtualView is Popup popup && popup.ShouldDismissOnOutsideClick())
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
		if (VirtualView is not null && PopupView.IsOpen && Content is not null)
		{
			// Find the actual content element within the composite popup and update its margin
			if (PopupView.Child is Grid container && container.Children.Count > 1)
			{
				var actualContent = container.Children.LastOrDefault() as FrameworkElement;
				if (actualContent != null)
				{
					// Recalculate layout using the unified method
					var (contentSize, x, y) = CalculateContentLayout(actualContent);

					// Apply the calculated size to the content for Fill layouts
					var horizontalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.HorizontalOptions);
					var verticalAlignment = PopupLayoutCalculator.GetLayoutAlignment(VirtualView.VerticalOptions);
					var isFillWidth = horizontalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;
					var isFillHeight = verticalAlignment == Microsoft.Maui.Primitives.LayoutAlignment.Fill;

					if (isFillWidth)
					{
						actualContent.Width = contentSize.Width;
					}

					if (isFillHeight)
					{
						actualContent.Height = contentSize.Height;
					}

					// Update the content's margin to reposition it
					actualContent.Margin = new Microsoft.UI.Xaml.Thickness(x, y, 0, 0);
				}
			}
		}
	}


}

#endif