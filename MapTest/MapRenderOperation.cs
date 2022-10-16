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
    private SKRect _mapObjectsBounds;
    private SKPoint _viewPortCenter;

    public MapRenderOperation()
    {
        _crossHairPaint = new SKPaint { Color = SKColor.Parse("#FF0000"), Style = SKPaintStyle.Fill };

        // This is to ensure we always have a bitmap to work with
        _bitmap = CreateBitmapFromControlBounds();

        MapObjects = new ObservableCollection<MapObject>();
        MapObjects.CollectionChanged += (_, _) =>
        {
            _bitmap = CreateBitmapFromMapObjectsBounds();

            if (!Bounds.IsEmpty && _bitmap.Width > Bounds.Width)
            {
                AdjustZoomLevelToBitmapBounds();
            }
        };
    }

    private void AdjustZoomLevelToBitmapBounds()
    {
        ZoomLevel = CalculateScale(
            (float)Bounds.Width,
            (float)Bounds.Height,
            _bitmap.Width,
            _bitmap.Height);
    }

    public Rect Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;

            _viewPortCenter = new SKPoint((float)_bounds.Width / 2, (float)_bounds.Height / 2);

            InitializeBitmap();
        }
    }

    public float ZoomLevel { get; set; } = 1;
    public float ZoomX { get; set; }
    public float ZoomY { get; set; }
    public bool ZoomExtent { get; set; }
    public bool CenterOnPosition { get; set; }
    public ObservableCollection<MapObject> MapObjects { get; set; }

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

        if (ZoomExtent && !string.IsNullOrEmpty(ZoomElementName))
        {
            // Hard code to the blue square
            var elementBounds = MapObjects.Single(o => o.Name == ZoomElementName).Bounds;
            var paddedElementBounds = Pad(elementBounds, 20);

            var zoomLevel = CalculateScale(
                (float)Bounds.Width, 
                (float)Bounds.Height, 
                paddedElementBounds.Width, 
                paddedElementBounds.Height);

            ZoomOnPoint(canvas, zoomLevel, paddedElementBounds.MidX, paddedElementBounds.MidY, true);
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

    private static float CalculateScale(float outerWidth, float outerHeight, float innerWidth, float innerHeight)
    {
        var scale = outerWidth / innerWidth;

        // Check whether the inner bounds are taller
        // than wide. If that's the case the scale
        // needs to be calculated using height instead.
        if (scale * innerHeight > outerHeight)
        {
            scale = outerHeight / innerHeight;
        }

        return scale;
    }

    private void RenderCrossHair(SKCanvas canvas)
    {
        canvas.DrawLine(500, 0, 500, 1000, _crossHairPaint);
        canvas.DrawLine(0, 500, 1000, 500, _crossHairPaint);
        canvas.DrawCircle(500, 500, 2, _crossHairPaint);
    }

    private void ZoomOnPoint(SKCanvas canvas, float zoomLevel, float x, float y, bool centerOnPosition)
    {
        // It looks like we're centering on the wrong position here but
        // have no fear, the actual centering happens with a translation
        // below.
        var scaleMatrix = centerOnPosition
            ? SKMatrix.CreateScale(zoomLevel, zoomLevel, 0, 0)
            : SKMatrix.CreateScale(zoomLevel, zoomLevel, x, y);

        var newBounds = scaleMatrix.MapRect(_mapObjectsBounds);
        if (newBounds.Width < Bounds.Width)
        {
            // Clip the lower zoom to ensure that you can't zoom out
            // further than the whole object being visible.
            AdjustZoomLevelToBitmapBounds();

            scaleMatrix = SKMatrix.CreateScale(ZoomLevel, ZoomLevel, x, y);

            // Ensure that when zooming out the bitmap never
            // appears away from the origin (top/left 0,0)
            // so that there won't be any gaps on screen.
            var topLeftMapped = scaleMatrix.MapPoint(_mapObjectsBounds.Left, _mapObjectsBounds.Top);

            if (topLeftMapped.X < 0 || topLeftMapped.Y < 0)
            {
                var scaleTranslateX = -Math.Min(0, topLeftMapped.X);
                var scaleTranslateY = -Math.Min(0, topLeftMapped.Y);

                if (scaleTranslateX != 0 || scaleTranslateY != 0)
                {
                    scaleMatrix = scaleMatrix.PostConcat(SKMatrix.CreateTranslation(scaleTranslateX, scaleTranslateY));
                }
            }
        }

        var matrix = canvas.TotalMatrix.PostConcat(scaleMatrix);

        if (centerOnPosition)
        {
            var mappedDesiredCenter = matrix.MapPoint(x, y);

            var translateX = mappedDesiredCenter.X - _viewPortCenter.X;
            var translateY = mappedDesiredCenter.Y - _viewPortCenter.Y;

            var translateMatrix = SKMatrix.CreateTranslation(-translateX, -translateY);

            matrix = matrix.PostConcat(translateMatrix);
        }

        canvas.SetMatrix(matrix);

        LogicalMatrix = new SKMatrix(canvas.TotalMatrix.Values);
    }

    public SKMatrix LogicalMatrix { get; private set; }
    public string? ZoomElementName { get; set; }

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

        _mapObjectsBounds = new SKRect(left, top, right, bottom);

        return new SKBitmap((int)_mapObjectsBounds.Width, (int)_mapObjectsBounds.Height, SKColorType.RgbaF16, SKAlphaType.Opaque);
    }
}