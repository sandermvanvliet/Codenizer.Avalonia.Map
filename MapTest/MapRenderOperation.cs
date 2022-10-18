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
    private readonly SKPaint _crossHairPaint;
    private readonly SKPaint _alternateCrossHairPaint;
    private SKBitmap _bitmap;
    private Rect _bounds;
    private SKMatrix _logicalMatrix = SKMatrix.Empty;
    private SKRect _mapObjectsBounds = SKRect.Empty;
    private SKRect _viewportBounds = SKRect.Empty;
    private string? _zoomElementName;
    private ZoomMode _zoomMode = ZoomMode.All;
    private SKPoint? _viewportCenterOn;

    public MapRenderOperation()
    {
        _crossHairPaint = new SKPaint { Color = SKColor.Parse("#FF0000"), Style = SKPaintStyle.Fill };
        _alternateCrossHairPaint = new SKPaint { Color = SKColor.Parse("#00FF00"), Style = SKPaintStyle.Fill };

        // This is to ensure we always have a bitmap to work with
        _bitmap = CreateBitmapFromControlBounds();

        MapObjects = new ObservableCollection<MapObject>();
        MapObjects.CollectionChanged += (_, _) =>
        {
            var originalBounds = _mapObjectsBounds;
            _mapObjectsBounds = CalculateTotalBoundsForMapObjects(MapObjects);

            if (Math.Abs(_mapObjectsBounds.Width - originalBounds.Width) > 0.1 ||
                Math.Abs(_mapObjectsBounds.Height - originalBounds.Height) > 0.1)
            {
                // Reset the zoom level
                ZoomLevel = 1;
            }

            _bitmap = CreateBitmapFromMapObjectsBounds();

            if (!_viewportBounds.IsEmpty && _bitmap.Width > _viewportBounds.Width)
            {
                // As we're re-initializing, ensure that the entire
                // bitmap will be visible in the viewport
                _zoomMode = ZoomMode.All;
            }

            if (!string.IsNullOrEmpty(_zoomElementName) && MapObjects.All(m => m.Name != _zoomElementName))
            {
                _zoomElementName = null;
            }
        };
    }

    public float ZoomLevel { get; private set; } = 1;
    public SKPoint ZoomCenter { get; private set; }
    public bool CenterOnPosition { get; private set; }
    public ObservableCollection<MapObject> MapObjects { get; set; }

    public Rect Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;

            // Bounds is an interface member of IDrawingOperation so
            // we need to keep that. However, for the sake of clarity
            // we will use viewport bounds instead.
            _viewportBounds = new SKRect(0, 0, (float)value.Width, (float)value.Height);

            InitializeBitmap();
        }
    }

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

    private void RenderCanvas(SKCanvas canvas)
    {
        canvas.Clear(CanvasBackgroundColor);

        canvas.Save();

        if (MapObjects.Any())
        {
            SKMatrix matrix;

            switch (_zoomMode)
            {
                case ZoomMode.Extent when !string.IsNullOrEmpty(_zoomElementName):
                    var elementBounds = MapObjects.Single(o => o.Name == _zoomElementName).Bounds;
                    matrix = CalculateMatrix.ForExtent(elementBounds, _viewportBounds, _mapObjectsBounds);
                    break;
                case ZoomMode.Point when Math.Abs(ZoomLevel - 1) > 0.01:
                    matrix = CalculateMatrix.ForPoint(ZoomLevel, ZoomCenter.X, ZoomCenter.Y, CenterOnPosition, _mapObjectsBounds, _viewportBounds, _viewportCenterOn.Value);
                    break;
                case ZoomMode.All:
                default:
                    matrix = CalculateMatrix.ToFitViewport(_viewportBounds, _mapObjectsBounds);
                    break;
            }

            canvas.SetMatrix(matrix);

            _logicalMatrix = new SKMatrix(canvas.TotalMatrix.Values);

            foreach (var mapObject in MapObjects)
            {
                mapObject.Render(canvas);
            }
        }

        canvas.Restore();

        RenderCrossHair(canvas);
        RenderAlternativeCrossHair(canvas);

        canvas.Flush();
    }

    private void RenderCrossHair(SKCanvas canvas)
    {
        canvas.DrawLine(_viewportBounds.MidX, 0, _viewportBounds.MidX, _viewportBounds.Height, _crossHairPaint);
        canvas.DrawLine(0, _viewportBounds.MidY, _viewportBounds.Width, _viewportBounds.MidY, _crossHairPaint);
        canvas.DrawCircle(_viewportBounds.MidX, _viewportBounds.MidY, 2, _crossHairPaint);
        canvas.DrawText($"{_viewportBounds.MidX}x{_viewportBounds.MidY}", new SKPoint(_viewportBounds.MidX, _viewportBounds.MidY), _crossHairPaint);
    }

    private void RenderAlternativeCrossHair(SKCanvas canvas)
    {
        canvas.DrawLine(_viewportBounds.MidX + 100, 0, _viewportBounds.MidX + 100, _viewportBounds.Height, _alternateCrossHairPaint);
        canvas.DrawLine(0, _viewportBounds.MidY -100, _viewportBounds.Width, _viewportBounds.MidY - 100, _alternateCrossHairPaint);
        canvas.DrawCircle(_viewportBounds.MidX + 100, _viewportBounds.MidY - 100, 2, _alternateCrossHairPaint);
        canvas.DrawText($"{_viewportBounds.MidX+100}x{_viewportBounds.MidY-100}", new SKPoint(_viewportBounds.MidX+100, _viewportBounds.MidY-100), _alternateCrossHairPaint);
    }

    private void InitializeBitmap()
    {
        _bitmap = MapObjects.Any()
            ? CreateBitmapFromMapObjectsBounds()
            : CreateBitmapFromControlBounds();

        // As we're re-initializing, ensure that the entire
        // bitmap will be visible in the viewport
        _zoomMode = ZoomMode.All;

        using var canvas = new SKCanvas(_bitmap);
        canvas.Clear(CanvasBackgroundColor);
    }

    private SKBitmap CreateBitmapFromControlBounds()
    {
        return new SKBitmap((int)_viewportBounds.Width, (int)_viewportBounds.Height, SKColorType.RgbaF16, SKAlphaType.Opaque);
    }

    private SKBitmap CreateBitmapFromMapObjectsBounds()
    {
        var width = _mapObjectsBounds.Width;
        var height = _mapObjectsBounds.Height;

        if (_mapObjectsBounds.Width < _viewportBounds.Width)
        {
            width = _viewportBounds.Width;
        }

        if (_mapObjectsBounds.Height < _viewportBounds.Height)
        {
            height = _viewportBounds.Height;
        }

        return new SKBitmap((int)width, (int)height, SKColorType.RgbaF16, SKAlphaType.Opaque);
    }

    private static SKRect CalculateTotalBoundsForMapObjects(ObservableCollection<MapObject> mapObjects)
    {
        var left = 0f;
        var top = 0f;
        var right = 0f;
        var bottom = 0f;

        foreach (var mapObject in mapObjects)
        {
            left = Math.Min(mapObject.Bounds.Left, left);
            top = Math.Min(mapObject.Bounds.Top, top);
            right = Math.Max(mapObject.Bounds.Right, right);
            bottom = Math.Max(mapObject.Bounds.Bottom, bottom);
        }

        return new SKRect(left, top, right, bottom);
    }

    public void Zoom(float level, SKPoint mapPosition, bool centerOnPosition, SKPoint viewportCenterOn)
    {
        _zoomMode = ZoomMode.Point;
        ZoomLevel = level;
        ZoomCenter = mapPosition;
        CenterOnPosition = centerOnPosition;
        _zoomElementName = null;
        _viewportCenterOn = viewportCenterOn;
    }

    public void ZoomAll()
    {
        _zoomMode = ZoomMode.All;
        ZoomLevel = 1;
        ZoomCenter = SKPoint.Empty;
        CenterOnPosition = false;
        _zoomElementName = null;
    }

    public void ZoomExtent(string elementName)
    {
        _zoomMode = ZoomMode.Extent;
        ZoomLevel = 1;
        ZoomCenter = SKPoint.Empty;
        CenterOnPosition = false;
        _zoomElementName = elementName;
    }

    public SKPoint MapViewportPositionToMapPosition(Avalonia.Point viewportPosition)
    {
        if (_logicalMatrix != SKMatrix.Empty)
        {
            // Because we want to get the _original_ coordinate on the
            // map before scaling or translation has happened we need
            // the inverse matrix.
            var inverseMatrix = _logicalMatrix.Invert();

            return inverseMatrix.MapPoint(new SKPoint((float)viewportPosition.X, (float)viewportPosition.Y));
        }

        return new SKPoint((float)viewportPosition.X, (float)viewportPosition.Y);
    }
}

public enum ZoomMode
{
    All,
    Extent,
    Point
}