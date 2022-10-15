using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MapTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            var zoomLevel = float.Parse(ZoomLevel.Text);
            var zoomX = float.Parse(ZoomX.Text);
            var zoomY = float.Parse(ZoomY.Text);

            Map.Zoom(zoomLevel, zoomX, zoomY, ZoomExtent.IsChecked.Value);
        }
    }
}
