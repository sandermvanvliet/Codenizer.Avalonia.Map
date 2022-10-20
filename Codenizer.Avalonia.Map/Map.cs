using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class Map : UserControl
{
    private readonly MapRenderOperation _renderOperation;
    private global::Avalonia.Point? _mouseWheelZoomingCapturedPositionOnViewport;
    private bool _isMouseWheelZooming;

    public static readonly DirectProperty<Map, ObservableCollection<MapObject>> MapObjectsProperty = AvaloniaProperty.RegisterDirect<Map, ObservableCollection<MapObject>>(nameof(MapObjects), map => map.MapObjects, (map, value) => map.MapObjects = value);
    public static readonly DirectProperty<Map, bool> ShowCrossHairProperty = AvaloniaProperty.RegisterDirect<Map, bool>(nameof(ShowCrossHair), map => map.ShowCrossHair, (map, value) => map.ShowCrossHair = value);

    public Map()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        IsHitTestVisible = true;

        _renderOperation = new MapRenderOperation();
        _renderOperation.MapObjects.CollectionChanged += (_, _) => InvalidateVisual();
        _renderOperation.RenderFinished += (_, args) =>
        {
            ZoomLevel = args.Scale;
        };
    }

    public float ZoomLevel { get; private set; } = 1;

    // This is a pass-through because otherwise we need to hook into
    // the collection changed events and propagate all changes to 
    // the render operation. I think that's a bit suboptimal so right
    // now it's like this. If it turns out there is some advantage
    // to hooking the event because can call InvalidateVisual() in
    // a better way then this will change.
    public ObservableCollection<MapObject> MapObjects
    {
        get => _renderOperation.MapObjects;
        set => _renderOperation.MapObjects = value;
    }

    public bool ShowCrossHair
    {
        get => _renderOperation.ShowCrossHair;
        set
        {
            if (value == _renderOperation.ShowCrossHair) return;
            _renderOperation.ShowCrossHair = value;
            RaisePropertyChanged(ShowCrossHairProperty, new Optional<bool>(!value), new BindingValue<bool>(value));
            InvalidateVisual();
        }
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
        var positionOnViewport = e.GetPosition(this);
        
        var mapPosition = _renderOperation.MapViewportPositionToMapPosition(positionOnViewport);
        
        // TODO: Add hit test for a map object here

        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        const double step = 0.1;

        var positionOnViewport = e.GetPosition(this);

        if (!_isMouseWheelZooming)
        {
            _isMouseWheelZooming = true;

            _mouseWheelZoomingCapturedPositionOnViewport = positionOnViewport;
        }

        var increment = e.Delta.Y == 0
            ? 0
            : e.Delta.Y > 0
                ? step
                : -step;

        var newZoomLevel = (float)(ZoomLevel + increment);
        
        if (newZoomLevel < 0.1)
        {
            newZoomLevel = 0.1f;
        }

        _renderOperation.Zoom(
            newZoomLevel,
            _renderOperation.MapViewportPositionToMapPosition(_mouseWheelZoomingCapturedPositionOnViewport!.Value),
            new SKPoint(
                (float)_mouseWheelZoomingCapturedPositionOnViewport.Value.X,
                (float)_mouseWheelZoomingCapturedPositionOnViewport.Value.Y));
        
        InvalidateVisual();

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isMouseWheelZooming)
        {
            _isMouseWheelZooming = false;
            _mouseWheelZoomingCapturedPositionOnViewport = null;
            e.Handled = true;
            return;
        }

        base.OnPointerMoved(e);
    }

    public void Zoom(float level, global::Avalonia.Point viewportPosition, string? elementName = null)
    {
        if (!string.IsNullOrEmpty(elementName))
        {
            _renderOperation.ZoomExtent(elementName);
        }
        else
        {
            var mapPosition = _renderOperation.MapViewportPositionToMapPosition(viewportPosition);
            _renderOperation.Zoom(level, mapPosition, new SKPoint((float)Bounds.Width / 2, (float)Bounds.Height / 2));
        }

        InvalidateVisual();
    }

    public void ZoomExtent(string elementName)
    {
        _renderOperation.ZoomExtent(elementName);

        InvalidateVisual();
    }

    public void ZoomAll()
    {
        _renderOperation.ZoomAll();

        InvalidateVisual();
    }
}