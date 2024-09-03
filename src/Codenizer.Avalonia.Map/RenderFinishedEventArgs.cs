// Copyright (c) 2024 Codenizer BV
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using Avalonia;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class RenderFinishedEventArgs : EventArgs
{
    public float Scale { get; }
    public TimeSpan RenderDuration { get; }
    public SKRect MapObjectsBounds { get; }
    public Rect ViewportBounds { get; }
    public SKRect? ExtentBounds { get; }
    public int NumberOfMapObjects { get; }
    
    public RenderFinishedEventArgs(float scale, TimeSpan renderDuration, SKRect mapObjectsBounds, Rect viewportBounds,
        SKRect? extentBounds, int numberOfMapObjects)
    {
        Scale = scale;
        RenderDuration = renderDuration;
        MapObjectsBounds = mapObjectsBounds;
        ViewportBounds = viewportBounds;
        ExtentBounds = extentBounds;
        NumberOfMapObjects = numberOfMapObjects;
    }
}
