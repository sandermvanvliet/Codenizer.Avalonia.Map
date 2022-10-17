using SkiaSharp;

namespace MapTest;

public class Path : MapObject
{
    private readonly SKPath _path;
    private readonly SKPaint _paint;

    public Path(string name, SKPoint[] points, string color)
    {
        Name = name;
        _path = new SKPath();
        _path.AddPoly(points, false);

        _paint = new SKPaint { Color = SKColor.Parse(color), Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
    }

    public override string Name { get; }
    public override SKRect Bounds => _path.Bounds;
    public override void Render(SKCanvas canvas)
    {
        canvas.DrawPath(_path, _paint);
    }
}