// Copyright (c) 2023 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class MapRenderOperation
{
    private static readonly SKColor CanvasBackgroundColor = SKColor.Parse("#FFFFFF");
    private readonly SKPaint _crossHairPaint;
    private readonly SKPaint _alternateCrossHairPaint;
    private Rect _bounds;
    private SKMatrix _logicalMatrix = SKMatrix.Empty;
    private SKMatrix _logicalMatrixInverted = SKMatrix.Empty;
    private SKRect _mapObjectsBounds = SKRect.Empty;
    private SKRect _viewportBounds = SKRect.Empty;
    private string? _zoomElementName;
    private ZoomMode _zoomMode = ZoomMode.All;
    private SKPoint? _viewportCenterOn;
    private float _zoomLevel = 1;
    private SKPoint _zoomCenter;
    private SKMatrix? _cachedMatrix;
    private SKRect? _elementBounds;

    public MapRenderOperation()
    {
        _crossHairPaint = new SKPaint { Color = SKColor.Parse("#FF0000"), Style = SKPaintStyle.Fill };
        _alternateCrossHairPaint = new SKPaint { Color = SKColor.Parse("#00FF00"), Style = SKPaintStyle.Fill };
        
        MapObjects = new ObservableCollection<MapObject>();
        MapObjects.CollectionChanged += (_, _) =>
        {
            var originalBounds = _mapObjectsBounds;
            _mapObjectsBounds = CalculateTotalBoundsForMapObjects(MapObjects);

            if (Math.Abs(_mapObjectsBounds.Width - originalBounds.Width) > 0.1 ||
                Math.Abs(_mapObjectsBounds.Height - originalBounds.Height) > 0.1)
            {
                // Reset the zoom level
                ZoomAll();
            }
            
            if (!string.IsNullOrEmpty(_zoomElementName) && MapObjects.All(m => m.Name != _zoomElementName))
            {
                _zoomElementName = null;
            }
        };
    }

    public event EventHandler<RenderFinishedEventArgs>? RenderFinished;

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

            _cachedMatrix = null;
        }
    }

    public bool ShowCrossHair { get; set; } = false;
    public RenderPriority RenderPriority { get; set; } = new NoExplicitRenderPriority();

    public void Render(IDrawingContextImpl context)
    {
        var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;

        if (canvas == null)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        if (Bounds.Width > 0)
        {
            canvas.Save();
            RenderCanvas(canvas);
            canvas.Restore();
        }

        stopwatch.Stop();

        RenderFinished?.Invoke(this, new RenderFinishedEventArgs(_logicalMatrix.ScaleX, stopwatch.Elapsed, _mapObjectsBounds, Bounds, _elementBounds, MapObjects.Count));
    }

    private void RenderCanvas(SKCanvas canvas)
    {
        canvas.Clear(CanvasBackgroundColor);

        canvas.Save();

        if (MapObjects.Any())
        {
            SKMatrix matrix;

            if (_cachedMatrix == null)
            {
                (matrix, _) = CalculateMatrixForZoomMode();
                _cachedMatrix = matrix;
            }
            else
            {
                matrix = _cachedMatrix.Value;
            }

            canvas.SetMatrix(matrix);

            if (_logicalMatrix != canvas.TotalMatrix)
            {
                _logicalMatrix = new SKMatrix(canvas.TotalMatrix.Values);
                _logicalMatrixInverted = _logicalMatrix.Invert();
            }

            foreach (var mapObject in MapObjects.OrderBy(mo => mo, RenderPriority))
            {
                mapObject.Render(canvas);
            }
        }

        canvas.Restore();

        if (ShowCrossHair)
        {
            RenderCrossHair(canvas);
            RenderAlternativeCrossHair(canvas);
        }

        canvas.Flush();
    }

    private (SKMatrix matrix, SKRect newBounds) CalculateMatrixForZoomMode()
    {
        SKMatrix matrix;
        SKRect newBounds;

        switch (_zoomMode)
        {
            case ZoomMode.Extent when !string.IsNullOrEmpty(_zoomElementName):
                var mapObject = MapObjects.SingleOrDefault(o => o.Name == _zoomElementName);
                if (mapObject == null)
                {
                    // If the object doesn't exist anymore then revert to zoom all
                    _zoomElementName = null;
                    matrix = CalculateMatrix.ToFitViewport(_viewportBounds, _mapObjectsBounds);
                    newBounds = _mapObjectsBounds;
                }
                else
                {
                    newBounds = CalculateMatrix.Round(mapObject.Bounds);
                    if (newBounds == SKRect.Empty)
                    {
                        matrix = CalculateMatrix.ToFitViewport(_viewportBounds, _mapObjectsBounds);
                        newBounds = _mapObjectsBounds;
                    }
                    else
                    {
                        matrix = CalculateMatrix.ForExtent(newBounds, _viewportBounds, _mapObjectsBounds);
                    }
                }

                break;
            case ZoomMode.Point when Math.Abs(_zoomLevel - 1) > 0.01 && _viewportCenterOn != null:
                (matrix, newBounds) = CalculateMatrix.ForPoint(_zoomLevel, _zoomCenter.X, _zoomCenter.Y, _mapObjectsBounds,
                    _viewportBounds, _viewportCenterOn.Value);
                break;
            case ZoomMode.All:
            default:
                matrix = CalculateMatrix.ToFitViewport(_viewportBounds, _mapObjectsBounds);
                newBounds = _mapObjectsBounds;
                break;
        }
        
        return (matrix, newBounds);
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
        canvas.DrawLine(0, _viewportBounds.MidY - 100, _viewportBounds.Width, _viewportBounds.MidY - 100, _alternateCrossHairPaint);
        canvas.DrawCircle(_viewportBounds.MidX + 100, _viewportBounds.MidY - 100, 2, _alternateCrossHairPaint);
        canvas.DrawText($"{_viewportBounds.MidX + 100}x{_viewportBounds.MidY - 100}", new SKPoint(_viewportBounds.MidX + 100, _viewportBounds.MidY - 100), _alternateCrossHairPaint);
    }

    private static SKRect CalculateTotalBoundsForMapObjects(ObservableCollection<MapObject> mapObjects)
    {
        double left = 0f;
        double top = 0f;
        double right = 0f;
        double bottom = 0f;

        foreach (var mapObject in mapObjects)
        {
            left = Math.Min(mapObject.Bounds.Left, left);
            top = Math.Min(mapObject.Bounds.Top, top);
            right = Math.Max(mapObject.Bounds.Right, right);
            bottom = Math.Max(mapObject.Bounds.Bottom, bottom);
        }

        return new SKRect((float)left, (float)top, (float)right, (float)bottom);
    }

    public SKRect Zoom(float level, SKPoint mapPosition, SKPoint viewportCenterOn)
    {
        _zoomMode = ZoomMode.Point;
        _zoomLevel = level;
        _zoomCenter = mapPosition;
        _zoomElementName = null;
        _viewportCenterOn = viewportCenterOn;
        _elementBounds = null;

        (_cachedMatrix, var newBounds) = CalculateMatrixForZoomMode();

        return newBounds;
    }

    public SKRect ZoomAll()
    {
        _zoomMode = ZoomMode.All;
        _zoomLevel = 1;
        _zoomCenter = SKPoint.Empty;
        _zoomElementName = null;
        _elementBounds = null;

        (_cachedMatrix, var newBounds) = CalculateMatrixForZoomMode();

        return newBounds;
    }

    public SKRect ZoomExtent(string elementName)
    {
        _zoomMode = ZoomMode.Extent;
        _zoomLevel = 1;
        _zoomCenter = SKPoint.Empty;
        _zoomElementName = elementName;
        _elementBounds = null;

        (_cachedMatrix, _elementBounds) = CalculateMatrixForZoomMode();

        return _elementBounds.Value;
    }

    public SKPoint MapViewportPositionToMapPosition(global::Avalonia.Point viewportPosition)
    {
        if (_logicalMatrixInverted != SKMatrix.Empty)
        {
            // Because we want to get the _original_ coordinate on the
            // map before scaling or translation has happened we need
            // the inverse matrix.
            return _logicalMatrixInverted.MapPoint(new SKPoint((float)viewportPosition.X, (float)viewportPosition.Y));
        }

        return new SKPoint((float)viewportPosition.X, (float)viewportPosition.Y);
    }

    public global::Avalonia.Point MapPositionToViewport(SKPoint mapPosition)
    {
        var mapped = _logicalMatrix.MapPoint(mapPosition);

        return new global::Avalonia.Point(mapped.X, mapped.Y);
    }

    public SKRect MapBoundsToViewport(SKRect elementBounds)
    {
        return _logicalMatrix.MapRect(elementBounds);
    }

    public void Pan(float panX, float panY)
    {
        // Setting the cached matrix ensures that it
        // will be used on the next render operation.
        _cachedMatrix = CalculateMatrix.ForPan(_logicalMatrix, panX, panY, _viewportBounds, _mapObjectsBounds);
    }
}
