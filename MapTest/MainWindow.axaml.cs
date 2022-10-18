using Avalonia.Controls;
using Avalonia.Interactivity;
using SkiaSharp;

namespace MapTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Squares1000()
        {
            Map.MapObjects.Add(new Square("redSquare", 0, 0, 1000, 1000, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 100, 100, 800, 800, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 400, 400, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 700, 200, 100, 100, "#FFCC00"));

            Map.MapObjects.Add(new Point("point1", 100, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 400, 400, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 700, 200, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 750, 250, 2, "#000000"));
        }

        private void Squares1100()
        {
            Map.MapObjects.Add(new Square("redSquare", 0, 0, 1100, 1100, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 100, 100, 800, 800, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 400, 400, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 700, 200, 100, 100, "#FFCC00"));

            Map.MapObjects.Add(new Point("point1", 100, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 400, 400, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 700, 200, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 750, 250, 2, "#000000"));
        }

        private void SquaresWithNegativeFromOrigin()
        {
            Map.MapObjects.Add(new Square("redSquare", 0, 0, 1100, 1100, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 100, 100, 800, 800, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 400, 400, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 700, 200, 100, 100, "#FFCC00"));
            Map.MapObjects.Add(new Square("purpleSquare", -100, -100, 100, 100, "#690fad"));
            
            Map.MapObjects.Add(new Point("point0", -50, -50, 2, "#FFFFFF"));
            Map.MapObjects.Add(new Point("point1", 100, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 400, 400, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 700, 200, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 750, 250, 2, "#000000"));
        }

        private void SquaresPortrait()
        {
            Map.MapObjects.Add(new Square("redSquare", 0, 0, 400, 1000, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 50, 100, 300, 800, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 100, 400, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 300, 200, 100, 100, "#FFCC00"));
                
            Map.MapObjects.Add(new Point("point1", 50, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 100, 400, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 300, 200, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 350, 250, 2, "#000000"));
        }

        private void SquaresLandscape()
        {
            Map.MapObjects.Add(new Square("redSquare", 0, 0, 1000, 400, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 100, 50, 800, 300, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 400, 100, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 200, 300, 100, 100, "#FFCC00"));
                
            Map.MapObjects.Add(new Point("point1", 100, 50, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 400, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 200, 300, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 250, 350, 2, "#000000"));
        }

        private void SquaresImage()
        {
            Map.MapObjects.Add(new Image("mapImage", 0, 0, 800, 400, $"avares://MapTest/map-watopia.png"));
            var points = new []
            {
                new SKPoint(100, 100),
                new SKPoint(100, 110),
                new SKPoint(110, 110),
                new SKPoint(110, 120)
            };
            Map.MapObjects.Add(new Path("path1", points, "#0000FF"));
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            var zoomLevel = float.Parse(ZoomLevel.Text);
            var zoomX = float.Parse(ZoomX.Text);
            var zoomY = float.Parse(ZoomY.Text);

            Map.Zoom(zoomLevel, true, new Avalonia.Point(zoomX, zoomY));
        }

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var x = e.AddedItems[0] as MapObject;
                Map.ZoomExtent(x.Name);
            }
        }

        private void RadioButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var value = (sender as RadioButton).Content as string;

            value = value.Trim().ToLower();

            Map.MapObjects.Clear();

            switch (value)
            {
                case "squares 1000":
                    Squares1000();
                    break;
                case "squares 1100":
                    Squares1100();
                    break;
                case "squares negative":
                    SquaresWithNegativeFromOrigin();
                    break;
                case "squares portrait":
                    SquaresPortrait();
                    break;
                case "squares landscape":
                    SquaresLandscape();
                    break;
                case "squares image":
                    SquaresImage();
                    break;
            }
        }

        private void ZoomAllButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Map.ZoomAll();
        }
    }
}
