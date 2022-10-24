namespace Codenizer.Avalonia.Map;

public class RenderFinishedEventArgs : EventArgs
{
    public float Scale { get; }
    public TimeSpan RenderDuration { get; }

    public RenderFinishedEventArgs(float scale, TimeSpan renderDuration)
    {
        Scale = scale;
        RenderDuration = renderDuration;
    }
}