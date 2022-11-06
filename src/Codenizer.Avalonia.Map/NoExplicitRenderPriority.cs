namespace Codenizer.Avalonia.Map;

public class NoExplicitRenderPriority : RenderPriority
{
    protected override int CompareCore(MapObject self, MapObject other)
    {
        return 0;
    }
}