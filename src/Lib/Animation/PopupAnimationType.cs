using System.ComponentModel;

namespace AppoMobi.Maui.FastPopups;

/// <summary>
/// Defines the animation types available for popup content.
/// Note: The background overlay always uses a fade animation synchronized with the content animation duration.
/// </summary>
public enum PopupAnimationType
{
    /// <summary>
    /// Platform-specific default animation (Fade on most platforms)
    /// </summary>
    [Description("Uses platform-specific default animation. The overlay fades while content animates.")]
    Default,

    /// <summary>
    /// No animation - instant display
    /// </summary>
    [Description("Displays instantly without any animation. Useful for quick interactions.")]
    None,

    /// <summary>
    /// Fade in/out (opacity 0→1)
    /// </summary>
    [Description("Simple fade in/out effect. Both overlay and content fade simultaneously.")]
    Fade,

    /// <summary>
    /// Zoom in from center (scale 0.5→1.0)
    /// </summary>
    [Description("Zooms in from center (scale 0.5→1.0). Creates focus on the popup content.")]
    ZoomIn,

    /// <summary>
    /// Zoom out to center (scale 1.5→1.0)
    /// </summary>
    [Description("Zooms out to center (scale 1.5→1.0). Dramatic entrance effect.")]
    ZoomOut,

    /// <summary>
    /// Combined bounce effect with scale overshoot on both axes (1.0→1.1→1.0)
    /// </summary>
    [Description("Combined bounce on both axes (0.5→1.1→1.0). Full squash and stretch effect.")]
    Bounce,

    /// <summary>
    /// Bounce effect with scale overshoot (1.0→1.1→1.0)
    /// </summary>
    [Description("Horizontal bounce with scale overshoot (0.5→1.1→1.0). Playful width squash effect.")]
    BounceHorizontal,

    /// <summary>
    /// Vertical bounce effect with scale overshoot (1.0→1.1→1.0)
    /// </summary>
    [Description("Vertical bounce with scale overshoot (0.5→1.1→1.0). Playful height squash effect.")]
    BounceVertical,

    /// <summary>
    /// Slide up from bottom (most common for modal dialogs)
    /// </summary>
    [Description("Slides up from bottom of screen. Most common animation for modal dialogs.")]
    FromBottom,

    /// <summary>
    /// Slide down from top
    /// </summary>
    [Description("Slides down from top of screen. Good for notifications or dropdown menus.")]
    FromTop,

    /// <summary>
    /// Slide in from right side
    /// </summary>
    [Description("Slides in from right side. Similar to iOS navigation push.")]
    FromRight,

    /// <summary>
    /// Slide in from left side
    /// </summary>
    [Description("Slides in from left side. Good for side panels or back navigation.")]
    FromLeft,

    /// <summary>
    /// Elastic slide from bottom with spring physics
    /// </summary>
    [Description("Slides from bottom with spring physics. Natural and satisfying motion.")]
    FromBottomElastic,

    /// <summary>
    /// Elastic slide from top with spring physics
    /// </summary>
    [Description("Slides from top with spring physics. Bouncy dropdown effect.")]
    FromTopElastic,

    /// <summary>
    /// Elastic slide from left with spring physics
    /// </summary>
    [Description("Slides from left with spring physics. Elastic side panel entrance.")]
    FromLeftElastic,

    /// <summary>
    /// Elastic slide from right with spring physics
    /// </summary>
    [Description("Slides from right with spring physics. Elastic side panel entrance.")]
    FromRightElastic,

    /// <summary>
    /// 3D flip animation rotating around Y axis (horizontal flip like card)
    /// </summary>
    [Description("3D horizontal flip like a card turning. Rotates 90° around Y-axis.")]
    FlipHorizontal,

    /// <summary>
    /// 3D flip animation rotating around X axis (vertical flip like trapdoor)
    /// </summary>
    [Description("3D vertical flip like a trapdoor. Rotates 90° around X-axis.")]
    FlipVertical,

    /// <summary>
    /// 1080° rotation with zoom (scale 0.3→1.0, extended duration)
    /// </summary>
    [Description("Rotates 1080° (3 full rotations) while zooming in. Very dramatic effect with extended duration.")]
    Whirl,

}
