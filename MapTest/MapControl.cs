using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MapTest;

public class MapControl : UserControl
{
    private readonly MapRenderOperation _renderOperation;

    public MapControl()
    {
        _renderOperation = new MapRenderOperation();
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

    public void Zoom(float level, float zoomX, float zoomY, bool zoomExtent)
    {
        _renderOperation.ZoomLevel = level;
        _renderOperation.ZoomX = zoomX;
        _renderOperation.ZoomY = zoomY;
        _renderOperation.ZoomExtent = zoomExtent;
        InvalidateVisual();
    }
}