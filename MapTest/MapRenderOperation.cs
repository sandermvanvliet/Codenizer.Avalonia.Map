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
            var originalBounds = _mapObjectsBounds;
            _mapObjectsBounds = CalculateTotalBoundsForMapObjects();

            if (Math.Abs(_mapObjectsBounds.Width - originalBounds.Width) > 0.1 ||
                Math.Abs(_mapObjectsBounds.Height - originalBounds.Height) > 0.1)
            {
                // Reset the zoom level
                ZoomLevel = 1;
            }

            _bitmap = CreateBitmapFromMapObjectsBounds();
            
            if (!Bounds.IsEmpty && _bitmap.Width > Bounds.Width)
            {
                AdjustZoomLevelToBitmapBounds();
            }

            if (!string.IsNullOrEmpty(ZoomElementName) && MapObjects.All(m => m.Name != ZoomElementName))
            {
                ZoomElementName = null;
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

    private void AdjustZoomLevelToMapObjectBounds()
    {
        if (_mapObjectsBounds.IsEmpty)
        {
            ZoomLevel = 1;
        }
        else
        {
            ZoomLevel = CalculateScale(
                (float)Bounds.Width,
                (float)Bounds.Height,
                _mapObjectsBounds.Width,
                _mapObjectsBounds.Height);
        }
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

    public float ZoomLevel { get; private set; } = 1;
    public SKPoint ZoomCenter { get; private set; }
    public bool CenterOnPosition { get; private set; }
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

        if (!string.IsNullOrEmpty(ZoomElementName))
        {
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
            ZoomOnPoint(canvas, ZoomLevel, ZoomCenter.X, ZoomCenter.Y, CenterOnPosition);
        }
        else
        {
            ZoomOnPoint(canvas, ZoomLevel, 0, 0, false);
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
        canvas.DrawLine(_viewPortCenter.X, 0, _viewPortCenter.X, (float)Bounds.Height, _crossHairPaint);
        canvas.DrawLine(0, _viewPortCenter.Y, (float)Bounds.Width, _viewPortCenter.Y, _crossHairPaint);
        canvas.DrawCircle(_viewPortCenter.X, _viewPortCenter.Y, 2, _crossHairPaint);
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
        if (newBounds.Width < Bounds.Width && newBounds.Height < Bounds.Height)
        {
            // Clip the lower zoom to ensure that you can't zoom out
            // further than the whole object being visible.
            AdjustZoomLevelToMapObjectBounds();

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

            // Update new bounds
            newBounds = scaleMatrix.MapRect(_mapObjectsBounds);
        }

        if (newBounds.Width < Bounds.Width)
        {
            // As we've already corrected for the aspect ratio
            // of the map objects bitmap, it turns out that
            // this one is indeed taller than wide.
            // To make it look nice we should center the
            // bitmap.
            var offset = (Bounds.Width - newBounds.Width) / 2;

            var translateMatrix = SKMatrix.CreateTranslation((float)offset, 0);

            scaleMatrix = scaleMatrix.PostConcat(translateMatrix);
        }

        if (newBounds.Height < Bounds.Height)
        {
            // As we've already corrected for the aspect ratio
            // of the map objects bitmap, it turns out that
            // this one is indeed wider than tall.
            // To make it look nice we should center the
            // bitmap.
            var offset = (Bounds.Height - newBounds.Height) / 2;

            var translateMatrix = SKMatrix.CreateTranslation(0, (float)offset);

            scaleMatrix = scaleMatrix.PostConcat(translateMatrix);
        }

        // Apply the scaling matrix
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
    public string? ZoomElementName { get; private set; }

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
        var width = _mapObjectsBounds.Width;
        var height = _mapObjectsBounds.Height;

        if (_mapObjectsBounds.Width < Bounds.Width)
        {
            width = (float)Bounds.Width;
        }

        if (_mapObjectsBounds.Height < Bounds.Height)
        {
            height = (float)Bounds.Height;
        }

        return new SKBitmap((int)width, (int)height, SKColorType.RgbaF16, SKAlphaType.Opaque);
    }

    private SKRect CalculateTotalBoundsForMapObjects()
    {
        var left = 0f;
        var top = 0f;
        var right = 0f;
        var bottom = 0f;

        foreach (var mapObject in MapObjects)
        {
            left = Math.Min(mapObject.Bounds.Left, left);
            top = Math.Min(mapObject.Bounds.Top, top);
            right = Math.Max(mapObject.Bounds.Right, right);
            bottom = Math.Max(mapObject.Bounds.Bottom, bottom);
        }

        return new SKRect(left, top, right, bottom);
    }

    public void Zoom(float level, SKPoint mapPosition, bool centerOnPosition, string? elementName)
    {
        ZoomLevel = level;
        ZoomCenter = mapPosition;
        CenterOnPosition = centerOnPosition;
        ZoomElementName = elementName;
    }

    public SKPoint MapViewportPositionToMapPosition(Avalonia.Point viewportPosition)
    {
        if (LogicalMatrix != SKMatrix.Empty)
        {
            // Because we want to get the _original_ coordinate on the
            // map before scaling or translation has happened we need
            // the inverse matrix.
            var inverseMatrix = LogicalMatrix.Invert();

            return inverseMatrix.MapPoint(new SKPoint((float)viewportPosition.X, (float)viewportPosition.Y));
        }

        return new SKPoint((float)viewportPosition.X, (float)viewportPosition.Y);
    }
}