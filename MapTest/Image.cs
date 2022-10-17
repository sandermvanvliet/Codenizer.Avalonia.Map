using System;
using Avalonia;
using Avalonia.Platform;
using SkiaSharp;

namespace MapTest;

public class Image : MapObject
{
    private readonly SKImage _image;

    public Image(string name, int x, int y, int width, int height, string resourceLocation)
    {
        Name = name;

        Bounds = new SKRect(x, y, x + width, y + height);

        var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
        var stream = assetLoader.Open(new Uri(resourceLocation));
        _image = SKImage.FromEncodedData(stream);
    }

    public override string Name { get; }
    public override SKPaint Paint { get; }
    public override SKRect Bounds { get; }
    public override void Render(SKCanvas canvas)
    {
        canvas.DrawImage(_image, Bounds);
    }
}