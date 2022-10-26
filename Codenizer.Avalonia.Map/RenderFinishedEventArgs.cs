using Avalonia;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class RenderFinishedEventArgs : EventArgs
{
    public float Scale { get; }
    public TimeSpan RenderDuration { get; }
    public SKRect MapObjectsBounds { get; }
    public Rect ViewportBounds { get; }
    public int NumberOfMapObjects { get; }
    
    public RenderFinishedEventArgs(float scale, TimeSpan renderDuration, SKRect mapObjectsBounds, Rect viewportBounds, int numberOfMapObjects)
    {
        Scale = scale;
        RenderDuration = renderDuration;
        MapObjectsBounds = mapObjectsBounds;
        ViewportBounds = viewportBounds;
        NumberOfMapObjects = numberOfMapObjects;
    }
}