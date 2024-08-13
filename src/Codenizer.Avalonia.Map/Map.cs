// Copyright (c) 2023 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class Map : Control
{
    private readonly MapRenderOperation _renderOperation;
    private global::Avalonia.Point? _mouseWheelZoomingCapturedPositionOnViewport;
    private bool _isMouseWheelZooming;

    public static readonly DirectProperty<Map, ObservableCollection<MapObject>> MapObjectsProperty = AvaloniaProperty.RegisterDirect<Map, ObservableCollection<MapObject>>(nameof(MapObjects), map => map.MapObjects, (map, value) => map.MapObjects = value);
    public static readonly DirectProperty<Map, bool> ShowCrossHairProperty = AvaloniaProperty.RegisterDirect<Map, bool>(nameof(ShowCrossHair), map => map.ShowCrossHair, (map, value) => map.ShowCrossHair = value);
    public static readonly DirectProperty<Map, bool> AllowUserZoomProperty = AvaloniaProperty.RegisterDirect<Map, bool>(nameof(AllowUserZoom), map => map.AllowUserZoom, (map, value) => map.AllowUserZoom = value);
    public static readonly DirectProperty<Map, bool> AllowUserPanProperty = AvaloniaProperty.RegisterDirect<Map, bool>(nameof(AllowUserPan), map => map.AllowUserPan, (map, value) => map.AllowUserPan = value);
    public static readonly DirectProperty<Map, bool> LogDiagnosticsProperty = AvaloniaProperty.RegisterDirect<Map, bool>(nameof(LogDiagnostics), map => map.LogDiagnostics, (map, value) => map.LogDiagnostics = value);

    private bool _isUpdating;
    private static readonly object SyncRoot = new();
    private UpdateScope? _updateScope;
    private bool _allowUserZoom = true;
    private bool _allowUserPan = true;
    private bool _logDiagnostics;
    private bool _isPanning;
    private global::Avalonia.Point? _previousViewportPanPosition;

    public event EventHandler<MapObjectSelectedEventArgs>? MapObjectSelected;
    public event EventHandler<MapDiagnosticsEventArgs>? DiagnosticsCaptured;

    public Map()
    {
        //Background = new SolidColorBrush(Colors.Transparent);
        IsHitTestVisible = true;
        ClipToBounds = true;

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

            if (LogDiagnostics)
            {
                DiagnosticsCaptured?.Invoke(this, new MapDiagnosticsEventArgs(args.RenderDuration, args.Scale, args.MapObjectsBounds, args.ViewportBounds, args.ExtentBounds));
            }
        };
        
        AffectsMeasure<Map>(MapObjectsProperty);
        AffectsRender<Map>(MapObjectsProperty);
        AffectsRender<Map>(AllowUserPanProperty);
        AffectsRender<Map>(AllowUserZoomProperty);
        AffectsRender<Map>(ShowCrossHairProperty);
    }

    private float ZoomLevel { get; set; } = 1;

    // This is a pass-through because otherwise we need to hook into
    // the collection changed events and propagate all changes to 
    // the render operation. I think that's a bit suboptimal so right
    // now it's like this. If it turns out there is some advantage
    // to hooking the event because can call InvalidateVisual() in
    // a better way then this will change.
    public ObservableCollection<MapObject> MapObjects
    {
        get => _renderOperation.MapObjects;
        set
        {
            var original = _renderOperation.MapObjects;
            _renderOperation.MapObjects = value;
            RaisePropertyChanged(MapObjectsProperty, original, value);
        }
    }

    public bool ShowCrossHair
    {
        get => _renderOperation.ShowCrossHair;
        set
        {
            if (value == _renderOperation.ShowCrossHair) return;
            _renderOperation.ShowCrossHair = value;
            RaisePropertyChanged(ShowCrossHairProperty, !value, value);

            InvalidateVisual();
        }
    }

    public bool AllowUserZoom
    {
        get => _allowUserZoom;
        set
        {
            if (value == _allowUserZoom) return;
            _allowUserZoom = value;
            RaisePropertyChanged(AllowUserZoomProperty, !value, value);

            InvalidateVisual();
        }
    }

    public bool AllowUserPan
    {
        get => _allowUserPan;
        set
        {
            if (value == _allowUserPan) return;
            _allowUserPan = value;
            
            RaisePropertyChanged(AllowUserPanProperty, !value, value);

            InvalidateVisual();
        }
    }

    public bool LogDiagnostics
    {
        get => _logDiagnostics;
        set
        {
            if (value == _logDiagnostics) return;
            _logDiagnostics = value;
            RaisePropertyChanged(LogDiagnosticsProperty, !value, value);

        }
    }

    public RenderPriority RenderPriority
    {
        get => _renderOperation.RenderPriority;
        set => _renderOperation.RenderPriority = value;
    }


    public IDisposable? BeginUpdate([CallerMemberName] string? caller = "")
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
        if (_isUpdating)
        {
            return;
        }

        context.Custom(_renderOperation);
        
        // if (_renderTarget != null)
        // {
        //     RenderMap(context as ImmediateDrawingContext);
        //
        //     context
        //         .DrawImage(
        //             _renderTarget,
        //             new Rect(0, 0, _renderTarget.PixelSize.Width, _renderTarget.PixelSize.Height),
        //             new Rect(0, 0, Bounds.Width, Bounds.Height));
        // }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = new Size(availableSize.Width, availableSize.Height);

        var visualParentBounds = this.GetVisualParent()?.Bounds;

        var width = desiredSize.Width;
        var height = desiredSize.Height;

        if (double.IsPositiveInfinity(width) && visualParentBounds is not null && !double.IsPositiveInfinity(visualParentBounds.Value.Width))
        {
            width = visualParentBounds.Value.Width;
        }

        if (double.IsPositiveInfinity(height) && visualParentBounds is not null && !double.IsPositiveInfinity(visualParentBounds.Value.Height))
        {
            height = visualParentBounds.Value.Height;
        }

        desiredSize = new Size(width, height);
        
        // Take all the space we can get
        return desiredSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        Debug.WriteLine($"Property {change.Property.Name} changed from {change.OldValue ?? "(null)"} to {change.NewValue}");
        
        if (change.Property.Name == nameof(Bounds))
        {
            // Always construct a new Rect without translation,
            // otherwise the rendering is offset _within_ the control
            // itself as the Bounds set on the control include the
            // left/top translation of the control to the parent (window).
            // For rendering we don't want that translation to happen
            // as we're drawing _inside_ of the control, not the parent.
            var newBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
            
            _renderOperation.Bounds = newBounds;

            InvalidateVisual();
        }

        base.OnPropertyChanged(change);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            e.Handled = true;
            return;
        }

        var positionOnViewport = e.GetPosition(this);

        var mapObject = FindMapObjectUnderCursor(positionOnViewport, true);

        if (mapObject != null)
        {
            MapObjectSelected?.Invoke(this, new MapObjectSelectedEventArgs(mapObject));
        }

        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (!AllowUserZoom)
        {
            return;
        }

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
        // End mouse wheel zoom when the pointer is moved
        // to prevent the zoom going all over the place
        if (_isMouseWheelZooming)
        {
            _isMouseWheelZooming = false;
            _mouseWheelZoomingCapturedPositionOnViewport = null;
            e.Handled = true;
            return;
        }

        if (AllowUserPan)
        {
            // Check to see what's underneath the cursor
            var currentPoint = e.GetCurrentPoint(this);
            var viewportPosition = currentPoint.Position;

            // Begin panning operation
            if (!currentPoint.Properties.IsLeftButtonPressed)
            {
                _isPanning = false;
                _previousViewportPanPosition = null;
                e.Handled = true;
                return;
            }

            if (!_isPanning && _previousViewportPanPosition == null)
            {
                _isPanning = true;
                _previousViewportPanPosition = viewportPosition;
            }
            else if (_isPanning)
            {
                Pan(viewportPosition);

                InvalidateVisual();

                e.Handled = true;
                return;
            }
        }

        base.OnPointerMoved(e);
    }

    private void Pan(global::Avalonia.Point viewportPosition)
    {
        if (_previousViewportPanPosition == null)
        {
            return;
        }
        
        // When a drag operation is active,
        // track the delta-x and delta-y values
        // based on the start position of the
        // drag operation

        var previousOnMap = _renderOperation.MapViewportPositionToMapPosition(_previousViewportPanPosition.Value);
        var currentOnMap = _renderOperation.MapViewportPositionToMapPosition(viewportPosition);

        var panX = (float)Math.Round(previousOnMap.X - currentOnMap.X, MidpointRounding.AwayFromZero);
        var panY = (float)Math.Round(previousOnMap.Y - currentOnMap.Y, MidpointRounding.AwayFromZero);

        _renderOperation.Pan(panX, panY);
        _previousViewportPanPosition = viewportPosition;
    }

    private MapObject? FindMapObjectUnderCursor(global::Avalonia.Point viewportPosition, bool forSelection)
    {
        var mapPosition = _renderOperation.MapViewportPositionToMapPosition(viewportPosition);

        var selectableObjects = MapObjects
            .Where(mo => (forSelection && mo.IsSelectable) || !forSelection)
            .ToList();

        var matchingObjects = selectableObjects
            .Where(mo => mo.Contains(mapPosition))
            .ToList();

        if (matchingObjects.Count == 0)
        {
            return null;
        }

        if (matchingObjects.Count == 1)
        {
            return matchingObjects.Single();
        }

        var matchingObject = matchingObjects
            .Where(mo => mo.TightContains(mapPosition))
            .MinBy(mo => mo.Bounds.Width * mo.Bounds.Height);

        return matchingObject;
    }

    public (SKRect newBounds, SKRect newBoundsMappedToViewport) Zoom(float level, global::Avalonia.Point viewportPosition)
    {
        var mapPosition = _renderOperation.MapViewportPositionToMapPosition(viewportPosition);
        var newBounds = _renderOperation.Zoom(level, mapPosition, new SKPoint((float)Bounds.Width / 2, (float)Bounds.Height / 2));
        var newBoundsMappedToViewport = CalculateMatrix.Round(_renderOperation.MapBoundsToViewport(newBounds));

        InvalidateVisual();

        return (newBounds, newBoundsMappedToViewport);
    }

    public (SKRect elementBounds, SKRect newBoundsMappedToViewport) ZoomExtent(string elementName)
    {
        var elementBounds = _renderOperation.ZoomExtent(elementName);
        var newBoundsMappedToViewport = CalculateMatrix.Round(_renderOperation.MapBoundsToViewport(elementBounds));

        InvalidateVisual();

        return (elementBounds, newBoundsMappedToViewport);
    }

    public (SKRect newBounds, SKRect newBoundsMappedToViewport) ZoomAll()
    {
        var newBounds = _renderOperation.ZoomAll();
        var newBoundsMappedToViewport = CalculateMatrix.Round(_renderOperation.MapBoundsToViewport(newBounds));

        InvalidateVisual();

        return (newBounds, newBoundsMappedToViewport);
    }

    public global::Avalonia.Point MapToViewport(SKPoint mapPosition)
    {
        return _renderOperation.MapPositionToViewport(mapPosition);
    }
}