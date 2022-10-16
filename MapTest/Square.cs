using SkiaSharp;

namespace MapTest;

public class Square : MapObject
{
    private readonly float _x;
    private readonly float _y;
    private readonly float _width;
    private readonly float _height;

    public Square(string name, float x, float y, float width, float height, string color)
    {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
        Name = name;
        Paint = new SKPaint { Color = SKColor.Parse(color), Style = SKPaintStyle.Fill };
        Bounds = new SKRect(x, y, x + width, y + height);
    }

    public override string Name { get; }
    public override SKPaint Paint { get; }

    public override SKRect Bounds { get; }

    public override void Render(SKCanvas canvas)
    {
        canvas.DrawRect(Bounds, Paint);
    }
}