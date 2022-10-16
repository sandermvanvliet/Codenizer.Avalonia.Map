using SkiaSharp;

namespace MapTest;

public abstract class MapObject
{
    public abstract string Name { get; }
    public abstract SKPaint Paint { get; }
    public abstract SKRect Bounds { get; }

    public abstract void Render(SKCanvas canvas);
}
