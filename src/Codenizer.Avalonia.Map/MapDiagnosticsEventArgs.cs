// Copyright (c) 2022 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using Avalonia;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class MapDiagnosticsEventArgs : EventArgs
{
    public MapDiagnosticsEventArgs(TimeSpan renderDuration)
    {
        RenderDuration = renderDuration;
    }

    public MapDiagnosticsEventArgs(TimeSpan renderDuration, float scale, SKRect mapObjectsBounds, Rect viewportBounds,
        SKRect? extentBounds)
    {
        RenderDuration = renderDuration;
        Scale = scale;
        MapObjectsBounds = new Rect(mapObjectsBounds.Left, mapObjectsBounds.Top, mapObjectsBounds.Width,
            mapObjectsBounds.Height);
        ViewportBounds = viewportBounds;

        if (extentBounds != null)
        {
            ExtentBounds = new Rect(extentBounds.Value.Left, extentBounds.Value.Top, extentBounds.Value.Width,
                extentBounds.Value.Height);
        }
    }

    public TimeSpan RenderDuration { get; }
    public float Scale { get; }
    public Rect MapObjectsBounds { get; }
    public Rect ViewportBounds { get; }

    public Rect? ExtentBounds { get; set; }
}
