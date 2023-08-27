// Copyright (c) 2023 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

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
    public override bool IsSelectable { get; set; } = true;
    public override bool IsVisible { get; set; } = true;

    protected override void RenderCore(SKCanvas canvas)
    {
        canvas.DrawPath(_path, _paint);
    }
}
