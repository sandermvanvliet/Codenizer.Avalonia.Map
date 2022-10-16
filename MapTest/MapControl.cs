using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace MapTest;

public class MapControl : UserControl
{
    private readonly MapRenderOperation _renderOperation;
    private SKPoint _mouseWheelZoomingCapturedPosition;
    private bool _isMouseWheelZooming;

    public static readonly DirectProperty<MapControl, ObservableCollection<MapObject>> HighlightedSegmentProperty = AvaloniaProperty.RegisterDirect<MapControl, ObservableCollection<MapObject>>(nameof(MapObjects), map => map.MapObjects, (map, value) => map.MapObjects = value);
    public MapControl()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        _renderOperation = new MapRenderOperation();
        _renderOperation.MapObjects.CollectionChanged += (_, _) => InvalidateVisual();
        IsHitTestVisible = true;
    }

    public ObservableCollection<MapObject> MapObjects
    {
        get => _renderOperation.MapObjects;
        set => _renderOperation.MapObjects = value;
    }

    public override void Render(DrawingContext context)
    {
        if (IsVisible)
        {
            context.Custom(_renderOperation);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Take all the space we can get
        return availableSize;
    }

    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        if (change.Property.Name == nameof(Bounds))
        {
            // Always construct a new Rect without translation,
            // otherwise the rendering is offset _within_ the control
            // itself as the Bounds set on the control include the
            // left/top translation of the control to the parent (window).
            // For rendering we don't want that translation to happen
            // as we're drawing _inside_ of the control, not the parent.
            _renderOperation.Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);

            InvalidateVisual();
        }

        base.OnPropertyChanged(change);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);

        _renderOperation.ZoomLevel = 2;
        
        var mappedPoint = new SKPoint((float)position.X, (float)position.Y);

        mappedPoint = ConvertPointOnControlToMapPosition(mappedPoint);

        Zoom(2, mappedPoint.X, mappedPoint.Y, false, true);
        
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        const double step = 0.05;

        var position = e.GetPosition(this);

        if (!_isMouseWheelZooming)
        {
            _isMouseWheelZooming = true;
            
            _mouseWheelZoomingCapturedPosition = new SKPoint((float)position.X, (float)position.Y);
            _mouseWheelZoomingCapturedPosition = ConvertPointOnControlToMapPosition(_mouseWheelZoomingCapturedPosition);
        }

        var increment = e.Delta.Y == 0
            ? 0
            : e.Delta.Y > 0
                ? step
                : -step;

        var newZoomLevel = (float)(_renderOperation.ZoomLevel + increment);
        
        if (newZoomLevel < 0.1)
        {
            newZoomLevel = 0.1f;
        }
        
        Zoom(newZoomLevel, _mouseWheelZoomingCapturedPosition.X, _mouseWheelZoomingCapturedPosition.Y, false, false);
        
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isMouseWheelZooming)
        {
            _isMouseWheelZooming = false;
            _mouseWheelZoomingCapturedPosition = SKPoint.Empty;
            e.Handled = true;
            return;
        }

        base.OnPointerMoved(e);
    }

    private SKPoint ConvertPointOnControlToMapPosition(SKPoint mappedPoint)
    {
        if (_renderOperation.LogicalMatrix != SKMatrix.Empty)
        {
            // Because we want to get the _original_ coordinate on the
            // map before scaling or translation has happened we need
            // the inverse matrix.
            var inverseMatrix = _renderOperation.LogicalMatrix.Invert();

            mappedPoint = inverseMatrix.MapPoint(mappedPoint);
        }

        return mappedPoint;
    }

    public void Zoom(float level, float zoomX, float zoomY, bool zoomExtent, bool centerOnPosition, string? elementName = null)
    {
        _renderOperation.ZoomLevel = level;
        _renderOperation.ZoomX = zoomX;
        _renderOperation.ZoomY = zoomY;
        _renderOperation.ZoomExtent = zoomExtent;
        _renderOperation.CenterOnPosition = centerOnPosition;
        _renderOperation.ZoomElementName = elementName;

        InvalidateVisual();
    }
}