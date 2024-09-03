// Copyright (c) 2024 Codenizer BV
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using System.Diagnostics;
using Avalonia.Threading;

namespace Codenizer.Avalonia.Map;

public class UpdateScope : IDisposable
{
    private Action? _endUpdate;
    private readonly object _syncRoot = new();
    private readonly Guid _id;

    public UpdateScope(Action endUpdate, string? caller)
    {
        _id = Guid.NewGuid();
        _endUpdate = endUpdate;
        Debug.WriteLine($"[UpdateScope({_id:D})] Start from {caller ?? "(unknown)"}");
    }

    public void Dispose()
    {
        Action? endAction = null;

        lock (_syncRoot)
        {
            if (_endUpdate != null)
            {
                endAction = _endUpdate;
                _endUpdate = null;
            }
        }

        if (endAction != null)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Debug.WriteLine($"[UpdateScope({_id:D})] Dispatching on UI thread");
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Debug.WriteLine($"[UpdateScope({_id:D})] Invoking on UI thread");
                    endAction.Invoke();
                });
            }
            else
            {
                Debug.WriteLine($"[UpdateScope({_id:D})] Invoking direct");
                endAction.Invoke();
            }
        }
        else
        {
            Debug.WriteLine($"[UpdateScope({_id:D})] Already ended");
        }
    }
}
