using System.ComponentModel;

namespace AppoMobi.Maui.FastPopups;

/// <summary>
/// Defines how the popup should be displayed in relation to system UI (status bar, navigation bar).
/// </summary>
public enum PopupDisplayMode
{
	/// <summary>
	/// Content respects safe area insets (status bar, navigation bar, notch, etc.).
	/// System UI remains visible. Content is positioned within safe areas for optimal usability.
	/// </summary>
	[Description("Content respects safe area insets. System UI visible, content positioned within safe areas.")]
	Default,

	/// <summary>
	/// Content extends edge-to-edge like the overlay, but system UI remains visible.
	/// Useful for drawer menus and full-screen content that should be visible behind system UI.
	/// Content may be obscured by status/navigation bars - design accordingly.
	/// </summary>
	[Description("Content extends edge-to-edge. System UI visible but content may be behind it.")]
	Cover,

	/// <summary>
	/// True fullscreen mode - hides system UI (status bar, navigation bar).
	/// Content extends edge-to-edge. Best for video players, immersive experiences.
	/// </summary>
	[Description("Hides system UI completely. Content extends edge-to-edge. For immersive experiences.")]
	FullScreen
}
