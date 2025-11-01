namespace AppoMobi.Maui.FastPopups;

/// <summary>
/// Defines the easing/interpolation curves for popup animations.
/// Controls how the animation accelerates and decelerates over time.
/// </summary>
public enum PopupAnimationEasing
{
    /// <summary>
    /// Platform-specific default (typically Decelerate for show, Accelerate for hide)
    /// </summary>
    Default = 0,

    /// <summary>
    /// Constant speed throughout the animation
    /// </summary>
    Linear = 1,

    /// <summary>
    /// Fast start, slow end (ease-out)
    /// </summary>
    Decelerate = 2,

    /// <summary>
    /// Slow start, fast end (ease-in)
    /// </summary>
    Accelerate = 3,

    /// <summary>
    /// Slow-fast-slow S-curve (ease-in-out)
    /// </summary>
    AccelerateDecelerate = 4,

    /// <summary>
    /// Spring physics with overshoot
    /// </summary>
    Spring = 5,

    /// <summary>
    /// Elastic bounce effect
    /// </summary>
    Elastic = 6,
}
