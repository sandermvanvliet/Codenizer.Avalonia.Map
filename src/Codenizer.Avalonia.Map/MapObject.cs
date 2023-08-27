// Copyright (c) 2023 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public abstract class MapObject
{
    public abstract string Name { get; }
    public abstract SKRect Bounds { get; }
    public abstract bool IsSelectable { get; set; }
    public abstract bool IsVisible { get; set; }

    public void Render(SKCanvas canvas)
    {
        if (IsVisible)
        {
            RenderCore(canvas);
        }
    }

    protected abstract void RenderCore(SKCanvas canvas);

    public virtual bool Contains(SKPoint mapPosition)
    {
        return mapPosition.X >= Bounds.Left &&
               mapPosition.X <= Bounds.Right &&
               mapPosition.Y >= Bounds.Top &&
               mapPosition.Y <= Bounds.Bottom;
    }

    public virtual bool TightContains(SKPoint mapPosition)
    {
        return Contains(mapPosition);
    }
}
