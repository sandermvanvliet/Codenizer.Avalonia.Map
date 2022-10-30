using Avalonia;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public abstract class MapObject
{
    public abstract string Name { get; }
    public abstract SKRect Bounds { get; }
    public abstract bool IsSelectable { get; set; }
    public abstract bool IsVisible { get; set; }

    public void Render(SKCanvas canvas)
    {
        if (IsVisible)
        {
            RenderCore(canvas);
        }
    }

    protected abstract void RenderCore(SKCanvas canvas);

    public virtual bool Contains(SKPoint mapPosition)
    {
        return Bounds.Contains(mapPosition);
    }

    public virtual Vector DistanceTo(SKPoint mapPosition)
    {
        var centerBounds = new SKPoint(Bounds.MidX, Bounds.MidY);

        return new Vector(
            Math.Abs(centerBounds.X - mapPosition.X),
            Math.Abs(centerBounds.Y - mapPosition.Y));
    }
}
