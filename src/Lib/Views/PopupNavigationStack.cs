using System.Collections.Generic;
using System.Linq;

namespace AppoMobi.Maui.Popups;

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

    public void Push(Popup popup)
    {
        lock (_lock)
        {
            _popupStack.Push(popup);
        }
    }

    public Popup? Pop()
    {
        lock (_lock)
        {
            return _popupStack.TryPop(out var popup) ? popup : null;
        }
    }

    public Popup? Peek()
    {
        lock (_lock)
        {
            return _popupStack.TryPeek(out var popup) ? popup : null;
        }
    }

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