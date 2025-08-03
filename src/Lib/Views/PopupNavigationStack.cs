using System.Collections.Generic;
using System.Linq;

namespace AppoMobi.Maui.Popups;

public class PopupNavigationStack
{
    private readonly Stack<Popup> _popupStack = new();
    private readonly object _lock = new();

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
}