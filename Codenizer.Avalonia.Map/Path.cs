using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class Path : MapObject
{
    private readonly SKPath _path;
    private readonly SKPaint _paint;

    public Path(string name, SKPoint[] points, string color, float strokeWidth = 2)
    {
        Name = name;
        _path = new SKPath();
        _path.AddPoly(points, false);

        _paint = new SKPaint { Color = SKColor.Parse(color), Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth };
    }

    public override string Name { get; }
    public override SKRect Bounds => _path.Bounds;
    public override void Render(SKCanvas canvas)
    {
        canvas.DrawPath(_path, _paint);
    }
}