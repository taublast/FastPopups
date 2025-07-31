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
		// Create a full-screen container
		var container = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		if (VirtualView == null)
		{
			return container;
		}


		// Create the full-screen overlay
		var backgroundColor = VirtualView.OverlayColor.ToWindowsColor();
		overlay = new BackgroundDimmer(() =>
		{
			if (CanBeDismissedByTappingOutside)
			{
				VirtualView?.OnDismissedByTappingOutsideOfPopup();
			}
		})
		{
			Background = new SolidColorBrush(backgroundColor),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		// Add overlay first (behind content)
		container.Children.Add(overlay);

		// Add actual content on top of overlay
		container.Children.Add(actualContent);

		return container;
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
		}

		PopupView.XamlRoot = this.XamlRoot;
		PopupView.IsOpen = true;

		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");
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


}

#endif