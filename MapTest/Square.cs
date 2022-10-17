using SkiaSharp;

namespace MapTest;

public class Square : MapObject
{
    private readonly SKPaint _paint;

    public Square(string name, float x, float y, float width, float height, string color)
    {
        Name = name;
        _paint = new SKPaint { Color = SKColor.Parse(color), Style = SKPaintStyle.Fill };
        Bounds = new SKRect(x, y, x + width, y + height);
    }

    public override string Name { get; }
    public override SKRect Bounds { get; }

    public override void Render(SKCanvas canvas)
    {
        canvas.DrawRect(Bounds, _paint);
    }
}