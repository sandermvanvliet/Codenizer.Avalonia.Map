using SkiaSharp;

namespace Codenizer.Avalonia.Map;

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
    public override bool IsSelectable { get; set; } = true;
    public override bool IsVisible { get; set; } = true;

    protected override void RenderCore(SKCanvas canvas)
    {
        canvas.DrawRect(Bounds, _paint);
    }
}