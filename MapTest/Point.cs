using SkiaSharp;

namespace MapTest;

public class Point : MapObject
{
    private readonly float _x;
    private readonly float _y;
    private readonly float _radius;
    private static readonly SKFont Font = new(SKTypeface.Default);
    private static readonly SKPaint TextPaint = new() { Color = SKColor.Parse("#000000"), IsAntialias = true };

    public Point(string name, float x, float y, float radius, string color)
    {
        _x = x;
        _y = y;
        _radius = radius;
        Name = name;
        Paint = new SKPaint { Color = SKColor.Parse(color), Style = SKPaintStyle.Fill };
        Bounds = new SKRect(x - radius, y - radius, x + radius, y + radius);
    }

    public override string Name { get; }
    public override SKPaint Paint { get; }
    public override SKRect Bounds { get; }
    public override void Render(SKCanvas canvas)
    {
        canvas.DrawCircle(_x, _y, _radius, Paint);
        canvas.DrawText($"{_x}x{_y}", _x, _y, Font, TextPaint);
    }
}