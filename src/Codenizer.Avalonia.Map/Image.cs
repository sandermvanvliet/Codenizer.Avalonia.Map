// Copyright (c) 2024 Codenizer BV
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using Avalonia;
using Avalonia.Platform;
using SkiaSharp;

namespace Codenizer.Avalonia.Map;

public class Image : MapObject
{
    private readonly SKImage _image;

    public Image(string name, int x, int y, int width, int height, string resourceLocation)
    {
        Name = name;

        Bounds = new SKRect(x, y, x + width, y + height);

        var stream = AssetLoader.Open(new Uri(resourceLocation));
        _image = SKImage.FromEncodedData(stream);
    }

    public override string Name { get; }
    public override SKRect Bounds { get; }
    public override bool IsSelectable { get; set; } = false;
    public override bool IsVisible { get; set; } = true;

    protected override void RenderCore(SKCanvas canvas)
    {
        canvas.DrawImage(_image, Bounds);
    }
}
