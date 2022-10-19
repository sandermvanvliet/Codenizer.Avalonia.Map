using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public abstract class MapObject
{
    public abstract string Name { get; }
    public abstract SKRect Bounds { get; }

    public abstract void Render(SKCanvas canvas);
}
