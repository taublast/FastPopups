using System.Collections.Generic;
using System.Linq;

namespace FastPopups;

/// <summary>
/// Manages a global stack of currently displayed popups.
/// This singleton class provides thread-safe operations for managing popup navigation,
/// ensuring proper ordering when multiple popups are shown.
/// </summary>
/// <remarks>
/// <para>
/// The stack follows Last-In-First-Out (LIFO) ordering, where the most recently shown popup
/// is at the top of the stack. This allows proper handling of nested or sequential popups.
/// </para>
/// <para>
/// All operations are thread-safe and can be safely called from any thread.
/// </para>
/// <example>
/// Basic usage:
/// <code>
/// // Show a popup
/// var popup = new Popup { Content = myView };
/// PopupNavigationStack.Instance.Push(popup);
/// await popup.Show();
///
/// // Close the top popup
/// PopupNavigationStack.Instance.CloseTop();
///
/// // Get current popup count
/// var count = PopupNavigationStack.Instance.Count;
/// </code>
/// </example>
/// </remarks>
public class PopupNavigationStack
{
    private static readonly Lazy<PopupNavigationStack> _instance = new(() => new PopupNavigationStack());

    /// <summary>
    /// Gets the global singleton instance of the popup navigation stack.
    /// </summary>
    public static PopupNavigationStack Instance => _instance.Value;

    private readonly Stack<Popup> _popupStack = new();
    private readonly object _lock = new();

    // Private constructor to enforce singleton pattern
    private PopupNavigationStack() { }

    /// <summary>
    /// Gets the number of popups currently in the navigation stack.
    /// </summary>
    /// <value>The total count of active popups.</value>
    /// <remarks>This operation is thread-safe.</remarks>
    public int Count 
    { 
        get 
        { 
            lock (_lock) 
            { 
                return _popupStack.Count; 
            } 
        } 
    }

    /// <summary>
    /// Adds a popup to the top of the navigation stack.
    /// </summary>
    /// <param name="popup">The popup to add to the stack.</param>
    /// <remarks>
    /// <para>This operation is thread-safe.</para>
    /// <para>
    /// Typically called automatically when a popup is shown. You generally don't need to call this manually
    /// unless implementing custom popup show logic.
    /// </para>
    /// </remarks>
    public void Push(Popup popup)
    {
        lock (_lock)
        {
            _popupStack.Push(popup);
        }
    }

    /// <summary>
    /// Removes and returns the topmost popup from the navigation stack.
    /// </summary>
    /// <returns>The popup that was removed from the top of the stack, or <see langword="null"/> if the stack is empty.</returns>
    /// <remarks>
    /// <para>This operation is thread-safe.</para>
    /// <para>
    /// This method does NOT close the popup - it only removes it from the stack.
    /// Use <see cref="CloseTop"/> if you want to both remove and close the popup.
    /// </para>
    /// </remarks>
    public Popup? Pop()
    {
        lock (_lock)
        {
            return _popupStack.TryPop(out var popup) ? popup : null;
        }
    }

    /// <summary>
    /// Returns the topmost popup from the navigation stack without removing it.
    /// </summary>
    /// <returns>The popup at the top of the stack, or <see langword="null"/> if the stack is empty.</returns>
    /// <remarks>This operation is thread-safe.</remarks>
    public Popup? Peek()
    {
        lock (_lock)
        {
            return _popupStack.TryPeek(out var popup) ? popup : null;
        }
    }

    /// <summary>
    /// Closes and removes all popups from the navigation stack.
    /// </summary>
    /// <remarks>
    /// <para>This operation is thread-safe.</para>
    /// <para>
    /// This method calls <see cref="Popup.Close(object)"/> on each popup in the stack,
    /// from top to bottom, and clears the entire stack.
    /// </para>
    /// <para>
    /// Use this when you need to dismiss all active popups, such as during navigation
    /// to a completely different section of your app.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            while (_popupStack.TryPop(out var popup))
            {
                popup?.Close();
            }
        }
    }

    /// <summary>
    /// Closes the topmost popup in the navigation stack with an optional result.
    /// </summary>
    /// <param name="result">The result value to pass to the popup's <see cref="Popup.Result"/> task. Defaults to <see langword="null"/>.</param>
    /// <remarks>
    /// <para>This operation is thread-safe.</para>
    /// <para>
    /// This is equivalent to calling <c>PopupNavigationStack.Instance.Peek()?.Close(result)</c>,
    /// but provides a convenient single-call API.
    /// </para>
    /// <para>
    /// If the stack is empty, this method does nothing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Close top popup without a result
    /// PopupNavigationStack.Instance.CloseTop();
    ///
    /// // Close top popup with a result
    /// PopupNavigationStack.Instance.CloseTop("User clicked OK");
    /// </code>
    /// </example>
    public void CloseTop(object? result = null)
    {
        lock (_lock)
        {
            var topPopup = Peek();
            if (topPopup != null)
            {
                topPopup.Close(result);
            }
        }
    }


    /// <summary>
    /// Removes a specific popup from the stack (used when popup closes naturally)
    /// </summary>
    /// <param name="popup">The popup to remove</param>
    public void Remove(Popup popup)
    {
        lock (_lock)
        {
            // Convert stack to list, remove the popup, and rebuild the stack
            var popups = _popupStack.ToArray();
            _popupStack.Clear();

            // Add back all popups except the one we want to remove
            // Reverse the array to maintain the original stack order
            foreach (var p in popups.Reverse())
            {
                if (p != popup)
                {
                    _popupStack.Push(p);
                }
            }
        }
    }
}