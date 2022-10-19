namespace Codenizer.Avalonia.Map;

public class RenderFinishedEventArgs : EventArgs
{
    public float Scale { get; }

    public RenderFinishedEventArgs(float scale)
    {
        Scale = scale;
    }
}