namespace Codenizer.Avalonia.Map;

public class MapObjectSelectedEventArgs : EventArgs
{
    public MapObject MapObject { get; }

    public MapObjectSelectedEventArgs(MapObject mapObject)
    {
        MapObject = mapObject;
    }
}