// Copyright (c) 2023 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

namespace Codenizer.Avalonia.Map;

public class MapObjectSelectedEventArgs : EventArgs
{
    public MapObject MapObject { get; }

    public MapObjectSelectedEventArgs(MapObject mapObject)
    {
        MapObject = mapObject;
    }
}
