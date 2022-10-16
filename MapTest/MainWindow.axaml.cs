using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MapTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Map.MapObjects.Add(new Square("redSquare", 0, 0, 1000, 1000, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 100, 100, 800, 800, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 400, 400, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 700, 200, 100, 100, "#FFCC00"));

            Map.MapObjects.Add(new Point("point1", 100, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 400, 400, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 700, 200, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 750, 250, 2, "#000000"));
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            var zoomLevel = float.Parse(ZoomLevel.Text);
            var zoomX = float.Parse(ZoomX.Text);
            var zoomY = float.Parse(ZoomY.Text);

            Map.Zoom(zoomLevel, zoomX, zoomY, ZoomExtent.IsChecked.Value, true);
        }
    }
}
