using System;

namespace MapTest;

public class RenderFinishedEventArgs : EventArgs
{
    public float Scale { get; }

    public RenderFinishedEventArgs(float scale)
    {
        Scale = scale;
    }
}