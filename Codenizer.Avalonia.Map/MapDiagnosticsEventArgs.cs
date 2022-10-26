namespace Codenizer.Avalonia.Map;

public class MapDiagnosticsEventArgs : EventArgs
{
    public TimeSpan RenderDuration { get; }

    public MapDiagnosticsEventArgs(TimeSpan renderDuration)
    {
        RenderDuration = renderDuration;
    }
}