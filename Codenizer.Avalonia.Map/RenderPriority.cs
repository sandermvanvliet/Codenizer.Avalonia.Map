﻿namespace Codenizer.Avalonia.Map;

public abstract class RenderPriority : IComparer<MapObject>
{
    public int Compare(MapObject? x, MapObject? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (ReferenceEquals(null, y))
        {
            return 1;
        }

        if (ReferenceEquals(null, x))
        {
            return -1;
        }

        return CompareCore(x, y);
    }

    protected abstract int CompareCore(MapObject self, MapObject other);
}