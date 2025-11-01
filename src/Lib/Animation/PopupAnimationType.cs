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
    Default = 0,

    /// <summary>
    /// No animation - instant display
    /// </summary>
    None = 1,

    /// <summary>
    /// Fade in/out (opacity 0→1)
    /// </summary>
    Fade = 2,

    /// <summary>
    /// Slide up from bottom (most common for modal dialogs)
    /// </summary>
    FromBottom = 3,

    /// <summary>
    /// Slide down from top
    /// </summary>
    FromTop = 4,

    /// <summary>
    /// Slide in from right side
    /// </summary>
    FromRight = 5,

    /// <summary>
    /// Slide in from left side
    /// </summary>
    FromLeft = 6,

    /// <summary>
    /// Zoom in from center (scale 0.5→1.0)
    /// </summary>
    ZoomIn = 7,

    /// <summary>
    /// Zoom out to center (scale 1.5→1.0)
    /// </summary>
    ZoomOut = 8,

    /// <summary>
    /// 180° rotation with zoom (scale 0.3→1.0)
    /// </summary>
    WhirlIn = 9,

    /// <summary>
    /// 1080° rotation with zoom (scale 0.3→1.0, extended duration)
    /// </summary>
    WhirlIn3 = 10,

    /// <summary>
    /// Bounce effect with scale overshoot (1.0→1.1→1.0)
    /// </summary>
    ShrinkHorizontal = 11,

    /// <summary>
    /// Elastic slide from bottom with spring physics
    /// </summary>
    FromBottomElastic = 12,
}
