// Copyright (c) 2025 Codenizer BV
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using Codenizer.Avalonia.Map;
using SkiaSharp;

namespace MapTest;

internal class FinishFlag : MapObject
{
    private readonly SKPaint _squarePaint;
    private readonly SKPaint _squarePaintAlternate;
    private readonly float _x;
    private readonly float _y;
    private SKPaint _circlePaint;

    public FinishFlag(string name, float x, float y, float width, float height, string color)
    {
        Name = name;
        Bounds = new SKRect(0, 0, width, height);

        _x = x;
        _y = y;
        _circlePaint = new SKPaint { Color = SKColor.Parse("#FFFFFF"), Style = SKPaintStyle.Stroke, StrokeWidth = 3};
        _squarePaint = new SKPaint { Color = SKColor.Parse(color), Style = SKPaintStyle.Fill };
        _squarePaintAlternate = new SKPaint { Color = SKColor.Parse("#FFFFFF"), Style = SKPaintStyle.Fill };
    }

    public override string Name { get; }
    public override SKRect Bounds { get; }
    public override bool IsSelectable { get; set; } = false;
    public override bool IsVisible { get; set; } = true;

    protected override void RenderCore(SKCanvas canvas)
    {
        var numberOfSquares = 5;
        var squareSize = Bounds.Width / numberOfSquares;

        canvas.DrawCircle(_x + Bounds.MidX, _y + Bounds.MidY, Bounds.MidX + squareSize, _squarePaint);

        for (var row = 0; row < numberOfSquares; row++)
        {
            for (var index = 0; index < numberOfSquares; index++)
            {
                canvas.DrawRect(
                    _x + index * squareSize, 
                    _y + row * squareSize, 
                    squareSize, 
                    squareSize,
                    index % 2 == row % 2 ? _squarePaint : _squarePaintAlternate);
            }
        }

        canvas.DrawCircle(_x + Bounds.MidX, _y + Bounds.MidY, Bounds.MidX + squareSize, _circlePaint);
    }
}
