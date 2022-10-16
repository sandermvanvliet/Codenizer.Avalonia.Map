using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace MapTest;

public class MapRenderOperation : ICustomDrawOperation
{
    private static readonly SKColor CanvasBackgroundColor = SKColor.Parse("#FFFFFF");
    private SKBitmap _bitmap;
    private Rect _bounds;
    private readonly SKPaint _crossHairPaint;

    public MapRenderOperation()
    {
        _crossHairPaint = new SKPaint { Color = SKColor.Parse("#FF0000"), Style = SKPaintStyle.Fill };

        // This is to ensure we always have a bitmap to work with
        _bitmap = CreateBitmapFromControlBounds();

        MapObjects = new ObservableCollection<MapObject>();
        MapObjects.CollectionChanged += (_, _) =>
        {
            _bitmap = CreateBitmapFromMapObjectsBounds();

            if(!Bounds.IsEmpty && _bitmap.Width > Bounds.Width)
            {
                AdjustZoomLevelToBitmapBounds();
            }
        };
    }

    private void AdjustZoomLevelToBitmapBounds()
    {
        var zoomLevel = (float)(Bounds.Width / _bitmap.Width);

        // Handle situation where the bitmap is taller than wide
        if (_bitmap.Height * zoomLevel > Bounds.Height)
        {
            zoomLevel = (float)(Bounds.Height / _bitmap.Height);
        }

        ZoomLevel = zoomLevel;
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
    public ObservableCollection<MapObject> MapObjects { get; }

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
        canvas.Save();

        canvas.Clear(CanvasBackgroundColor);

        if (ZoomExtent)
        {
            // Hard code to the blue square
            var yellowSquareBounds = MapObjects.Single(o => o.Name == "yellowSquare").Bounds;
            var bounds = Pad(yellowSquareBounds, 20);

            var zoomLevel = _bitmap.Width / bounds.Width;
            
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

        foreach (MapObject mapObject in MapObjects)
        {
            mapObject.Render(canvas);
        }

        canvas.Restore();

        RenderCrossHair(canvas);

        canvas.Flush();
    }

    private void RenderCrossHair(SKCanvas canvas)
    {
        canvas.DrawLine(500, 0, 500, 1000, _crossHairPaint);
        canvas.DrawLine(0, 500, 1000, 500, _crossHairPaint);
        canvas.DrawCircle(500, 500, 2, _crossHairPaint);
    }

    private void ZoomOnPoint(SKCanvas canvas, float zoomLevel, float x, float y, bool centerOnPosition)
    {
        var centerX = _bitmap.Width / 2;
        var centerY = _bitmap.Height / 2;

        // It looks like we're centering on the wrong position here but
        // have no fear, the actual centering happens with a translation
        // below.
        var scaleMatrix = centerOnPosition 
            ? SKMatrix.CreateScale(zoomLevel, zoomLevel, centerX, centerY) 
            : SKMatrix.CreateScale(zoomLevel, zoomLevel, x, y);
        
        var bitmapBounds = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
        var newBounds = scaleMatrix.MapRect(bitmapBounds);
        if (newBounds.Width < Bounds.Width)
        {
            // Clip the lower zoom to ensure that you can't zoom out
            // further than the whole object being visible.
            AdjustZoomLevelToBitmapBounds();

            // Copy the new zoom level because it's used below for
            // the translation step
            zoomLevel = ZoomLevel;
            
            scaleMatrix = SKMatrix.CreateScale(ZoomLevel, ZoomLevel, x, y);
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
        
        // Ensure that when zooming out the bitmap never
        // appears away from the origin (top/left 0,0)
        // so that there won't be any gaps on screen.
        var topLeft = matrix.MapPoint(0, 0);

        if (topLeft.X > 0 || topLeft.Y > 0)
        {
            var clipTranslateX = topLeft.X > 0 ? topLeft.X : 0;
            var clipTranslateY = topLeft.Y > 0 ? topLeft.Y : 0;
            var minZoomLevelTranslate = SKMatrix.CreateTranslation(-clipTranslateX, -clipTranslateY);
            matrix = matrix.PostConcat(minZoomLevelTranslate);
        }

        canvas.SetMatrix(matrix);

        LogicalMatrix = new SKMatrix(canvas.TotalMatrix.Values);
    }

    public SKMatrix LogicalMatrix { get; private set; }

    private static SKRect Pad(SKRect bounds, int padding)
    {
        return new SKRect(
            bounds.Left - padding,
            bounds.Top - padding,
            bounds.Right + padding,
            bounds.Bottom + padding);
    }

    public void Dispose()
    {
    }

    public bool HitTest(Avalonia.Point p)
    {
        return false;
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return false;
    }

    private void InitializeBitmap()
    {
        _bitmap = MapObjects.Any()
            ? CreateBitmapFromMapObjectsBounds()
            : CreateBitmapFromControlBounds();

        AdjustZoomLevelToBitmapBounds();

        using var canvas = new SKCanvas(_bitmap);
        canvas.Clear(CanvasBackgroundColor);
    }

    private SKBitmap CreateBitmapFromControlBounds()
    {
        return new SKBitmap((int)Bounds.Width, (int)Bounds.Height, SKColorType.RgbaF16, SKAlphaType.Opaque);
    }

    private SKBitmap CreateBitmapFromMapObjectsBounds()
    {
        var left = 0f;
        var top = 0f;
        var right = 0f;
        var bottom = 0f;

        foreach (var mapObject in MapObjects)
        {
            if (mapObject.Bounds.Left < left)
            {
                left = mapObject.Bounds.Left;
            }

            if (mapObject.Bounds.Top < top)
            {
                top = mapObject.Bounds.Top;
            }
            
            if (mapObject.Bounds.Right > right)
            {
                right = mapObject.Bounds.Right;
            }
            
            if (mapObject.Bounds.Bottom > bottom)
            {
                bottom = mapObject.Bounds.Bottom;
            }
        }

        var mapObjectsBounds = new SKRect(left, top, right, bottom);

        return new SKBitmap((int)mapObjectsBounds.Width, (int)mapObjectsBounds.Height, SKColorType.RgbaF16, SKAlphaType.Opaque);
    }
}