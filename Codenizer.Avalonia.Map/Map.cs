using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class Map : UserControl
{
    private readonly MapRenderOperation _renderOperation;
    private global::Avalonia.Point? _mouseWheelZoomingCapturedPositionOnViewport;
    private bool _isMouseWheelZooming;

    public static readonly DirectProperty<Map, ObservableCollection<MapObject>> MapObjectsProperty = AvaloniaProperty.RegisterDirect<Map, ObservableCollection<MapObject>>(nameof(MapObjects), map => map.MapObjects, (map, value) => map.MapObjects = value);
    public static readonly DirectProperty<Map, bool> ShowCrossHairProperty = AvaloniaProperty.RegisterDirect<Map, bool>(nameof(ShowCrossHair), map => map.ShowCrossHair, (map, value) => map.ShowCrossHair = value);
    private bool _isUpdating = false;
    private static readonly object SyncRoot = new();
    private UpdateScope? _updateScope;
    public event EventHandler<MapObjectSelectedEventArgs>? MapObjectSelected;

    public Map()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        IsHitTestVisible = true;

        _renderOperation = new MapRenderOperation();
        _renderOperation.MapObjects.CollectionChanged += (_, _) =>
        {
            if (!_isUpdating)
            {
                InvalidateVisual();
            }
        };
        _renderOperation.RenderFinished += (_, args) =>
        {
            ZoomLevel = args.Scale;
            Debug.WriteLine($"Render duration: {args.RenderDuration.TotalMilliseconds}ms");
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

    public RenderPriority RenderPriority
    {
        get => _renderOperation.RenderPriority;
        set => _renderOperation.RenderPriority = value;
    }

    
    public IDisposable? BeginUpdate([CallerMemberName]string? caller = "")
    {
        lock (SyncRoot)
        {
            if (!_isUpdating)
            {
                _isUpdating = true;

                _updateScope = new UpdateScope(EndUpdateAndRender, caller);
            }
        }

        return _updateScope;
    }

    private void EndUpdateAndRender()
    {
        lock (SyncRoot)
        {
            _isUpdating = false;
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (IsVisible)
        {
            lock (SyncRoot)
            {
                if (_isUpdating)
                {
                    Debug.WriteLine("Not rendering because currently updating");
                    return;
                }
            }
            
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
        
        var mapObject = FindMapObjectUnderCursor(positionOnViewport);

        if (mapObject != null)
        {
            MapObjectSelected?.Invoke(this, new MapObjectSelectedEventArgs(mapObject));
        }

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

        // Check to see what's underneath the cursor
        var viewportPosition = e.GetPosition(this);

        FindMapObjectUnderCursor(viewportPosition);

        base.OnPointerMoved(e);
    }

    private MapObject? FindMapObjectUnderCursor(global::Avalonia.Point viewportPosition)
    {
        var mapPosition = _renderOperation.MapViewportPositionToMapPosition(viewportPosition);

        var matchingObject = MapObjects
            .Where(mo => mo.Contains(mapPosition))
            .MinBy(mo => mo.Bounds.Width * mo.Bounds.Height);

        return matchingObject;
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