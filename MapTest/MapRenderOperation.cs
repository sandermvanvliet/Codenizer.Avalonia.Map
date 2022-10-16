using System;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace MapTest;

public class MapRenderOperation : ICustomDrawOperation
{
    private static readonly SKColor CanvasBackgroundColor = SKColor.Parse("#FFFFFF");
    private SKBitmap? _bitmap;
    private Rect _bounds;
    private readonly SKFont _font;
    private readonly SKPaint _textPaint;
    private readonly SKPaint _orangePaint;
    private readonly SKPaint _bluePaint;
    private readonly SKPaint _greenPaint;
    private readonly SKPaint _redPaint;

    public MapRenderOperation()
    {
        _font = new SKFont(SKTypeface.Default);
        _textPaint = new SKPaint { Color = SKColor.Parse("#000000") };
        _orangePaint = new SKPaint { Color = SKColor.Parse("#FFCC00"), Style = SKPaintStyle.Fill };
        _bluePaint = new SKPaint { Color = SKColor.Parse("#0000FF"), Style = SKPaintStyle.Fill };
        _greenPaint = new SKPaint { Color = SKColor.Parse("#00FF00"), Style = SKPaintStyle.Fill };
        _redPaint = new SKPaint { Color = SKColor.Parse("#FF0000"), Style = SKPaintStyle.Fill };
    }

    public Rect Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;

            InitializeBitmap();
        }
    }

    public float ZoomLevel { get; set; } = 1;
    public float ZoomX { get; set; }
    public float ZoomY { get; set; }
    public bool ZoomExtent { get; set; }
    public bool CenterOnPosition { get; set; }

    public void Render(IDrawingContextImpl context)
    {
        var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;

        if (canvas == null)
        {
            return;
        }

        canvas.Clear(CanvasBackgroundColor);

        if (_bitmap is { Width: > 0 })
        {
            using (var mapCanvas = new SKCanvas(_bitmap))
            {
                RenderCanvas(mapCanvas);
            }

            canvas.DrawBitmap(_bitmap, 0, 0);
        }
    }

    private void RenderCanvas(SKCanvas canvas)
    {
        canvas.Clear(CanvasBackgroundColor);

        canvas.Save();

        if (ZoomExtent)
        {
            // Hard code to the blue square
            var blueSquareBounds = new SKRect(400, 400, 600, 600);
            var yellowSquareBounds = new SKRect(700, 200, 800, 300);
            var bounds = Pad(yellowSquareBounds, 20);

            var ratio = Bounds.Width / bounds.Width;

            var zoomLevel = (float)ratio;
            
            ZoomOnPoint(canvas, zoomLevel, bounds.MidX, bounds.MidY, true);
        }
        else if (Math.Abs(ZoomLevel - 1) > 0.01)
        {
            ZoomOnPoint(canvas, ZoomLevel, ZoomX, ZoomY, CenterOnPosition);
        }
        else
        {
            LogicalMatrix = SKMatrix.Empty;
        }

        canvas.DrawRect(0, 0, 1000, 1000, _redPaint);
        canvas.DrawRect(100, 100, 800, 800, _greenPaint);
        canvas.DrawRect(400, 400, 200, 200, _bluePaint);
        canvas.DrawRect(700, 200, 100, 100, _orangePaint);

        DrawPointMarker(canvas, 100, 100);
        DrawPointMarker(canvas, 400, 400);
        DrawPointMarker(canvas, 750, 250);
        DrawPointMarker(canvas, 700, 200);

        canvas.Restore();

        canvas.DrawLine(500, 0, 500, 1000, _redPaint);
        canvas.DrawLine(0, 500, 1000, 500, _redPaint);
        canvas.DrawCircle(500,500,2, _redPaint);

        canvas.Flush();
    }

    private void ZoomOnPoint(SKCanvas canvas, float zoomLevel, float x, float y, bool centerOnPosition)
    {
        var centerX = _bitmap.Width / 2;
        var centerY = _bitmap.Height / 2;

        SKMatrix scaleMatrix;

        if(centerOnPosition)
        {
            scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, centerX, centerY);
        }
        else
        {
            scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, x, y);
        }

        var newBounds = scaleMatrix.MapRect(Bounds.ToSKRect());
        if (newBounds.Width < Bounds.Width)
        {
            // Clip the lower zoom to ensure that you can't zoom out
            // further than the whole object being visible.

            zoomLevel = (float)Bounds.Width / 1000;
            
            // Store the new zoom level so that the MapControl
            // can obtain it and use it for mouse wheel zoom
            // increments etc.
            ZoomLevel = zoomLevel;

            scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, x, y);
        }

        // This works:
        var translateX = centerX - x;
        var translateY = centerY - y;
        var translateMatrix = SKMatrix.CreateTranslation(zoomLevel * translateX, zoomLevel * translateY);

        var matrix = canvas.TotalMatrix.PostConcat(scaleMatrix);

        if (centerOnPosition)
        {
            matrix = matrix.PostConcat(translateMatrix);
        }

        canvas.SetMatrix(matrix);

        LogicalMatrix = new SKMatrix(canvas.TotalMatrix.Values);
    }

    public SKMatrix LogicalMatrix { get; private set; }

    private SKRect Pad(SKRect bounds, int padding)
    {
        return new SKRect(
            bounds.Left - padding,
            bounds.Top - padding,
            bounds.Right + padding,
            bounds.Bottom + padding);
    }

    private void DrawPointMarker(SKCanvas canvas, int x, int y)
    {
        canvas.DrawCircle(x, y, 2, _textPaint);
        canvas.DrawText($"{x}x{y}", x, y, _font, _textPaint);
    }

    public void Dispose()
    {
    }

    public bool HitTest(Point p)
    {
        return false;
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return false;
    }

    private void InitializeBitmap()
    {
        _bitmap = new SKBitmap((int)Bounds.Width, (int)Bounds.Height, SKColorType.RgbaF16, SKAlphaType.Opaque);

        using var canvas = new SKCanvas(_bitmap);
        canvas.Clear(CanvasBackgroundColor);
    }
}