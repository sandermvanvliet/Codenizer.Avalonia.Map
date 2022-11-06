// Copyright (c) 2022 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class Point : MapObject
{
    private readonly float _x;
    private readonly float _y;
    private readonly float _radius;
    private static readonly SKFont Font = new(SKTypeface.Default);
    private readonly SKPaint _textPaint;
    private readonly SKPaint _paint;

    public Point(string name, float x, float y, float radius, string color)
    {
        _x = x;
        _y = y;
        _radius = radius;
        Name = name;

        var parsedColor = SKColor.Parse(color);
        _paint = new SKPaint { Color = parsedColor, Style = SKPaintStyle.Fill };
        _textPaint = new SKPaint { Color = parsedColor, IsAntialias = true };
        
        // This excludes the text but that's exactly what we want
        // as the text is purely informational anyway and otherwise
        // we can't center on a point exactly
        Bounds = new SKRect(x - radius, y - radius, x + radius, y + radius);
    }

    public override string Name { get; }
    public override SKRect Bounds { get; }
    public override bool IsSelectable { get; set; } = true;
    public override bool IsVisible { get; set; } = true;

    protected override void RenderCore(SKCanvas canvas)
    {
        canvas.DrawCircle(_x, _y, _radius, _paint);
        canvas.DrawText($"{_x}x{_y}", _x, _y, Font, _textPaint);
    }
}
