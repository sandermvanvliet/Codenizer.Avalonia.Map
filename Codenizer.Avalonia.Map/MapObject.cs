using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public abstract class MapObject
{
    public abstract string Name { get; }
    public abstract SKRect Bounds { get; }

    public abstract void Render(SKCanvas canvas);

    public virtual bool Contains(SKPoint mapPosition)
    {
        return Bounds.Contains(mapPosition);
    }
}
