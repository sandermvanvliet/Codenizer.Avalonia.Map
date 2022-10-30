using Avalonia;

namespace Codenizer.Avalonia.Map;

internal class VectorComparer : IComparer<Vector>
{
    public int Compare(Vector x, Vector y)
    {
        return x.Length.CompareTo(y.Length);
    }
}