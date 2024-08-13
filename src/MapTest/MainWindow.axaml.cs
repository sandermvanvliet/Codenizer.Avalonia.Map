// Copyright (c) 2023 Sander van Vliet
// Licensed under GNU General Public License v3.0
// See LICENSE or https://choosealicense.com/licenses/gpl-3.0/

using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Codenizer.Avalonia.Map;
using SkiaSharp;
using Path = Codenizer.Avalonia.Map.Path;

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
            using var updateScope = Map.BeginUpdate();
            Map.MapObjects.Add(new Square("redSquare", 0, 0, 1000, 1000, "#FF0000"));
            Map.MapObjects.Add(new Square("greenSquare", 100, 100, 800, 800, "#00FF00"));
            Map.MapObjects.Add(new Square("blueSquare", 400, 400, 200, 200, "#0000FF"));
            Map.MapObjects.Add(new Square("yellowSquare", 700, 200, 100, 100, "#FFCC00"));
            
            Map.MapObjects.Add(new Square("lt", 1, 1, 20, 20, "#000000"));
            Map.MapObjects.Add(new Square("rt", 979, 1, 20, 20, "#000000"));
            Map.MapObjects.Add(new Square("lb", 1, 979, 20, 20, "#000000"));
            Map.MapObjects.Add(new Square("rb", 979, 979, 20, 20, "#000000"));

            Map.MapObjects.Add(new Point("point1", 100, 100, 2, "#000000"));
            Map.MapObjects.Add(new Point("point2", 400, 400, 2, "#000000"));
            Map.MapObjects.Add(new Point("point3", 700, 200, 2, "#000000"));
            Map.MapObjects.Add(new Point("point4", 750, 250, 2, "#000000"));
        }

        private void Squares1100()
        {
            using var updateScope = Map.BeginUpdate();
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
            using var updateScope = Map.BeginUpdate();
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
            using var updateScope = Map.BeginUpdate();
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
            using var updateScope = Map.BeginUpdate();
            Map.MapObjects.Add(new Codenizer.Avalonia.Map.Image("mapImage", 0, 0, 8192, 4096, $"avares://MapTest/map-watopia.png"));
            
            var worldMostLeft = new TrackPoint(-11.68401, 166.89304);
            var worldMostRight = new TrackPoint(-11.64594, 167.00275);
            var mapMostLeft = new Avalonia.Point(822, 3105);
            var mapMostRight = new Avalonia.Point(6618, 1067);

            var deltaLat = Math.Abs(worldMostRight.Latitude - worldMostLeft.Latitude);
            var deltaLon = Math.Abs(worldMostRight.Longitude - worldMostLeft.Longitude);

            var deltaMapX = Math.Abs(mapMostRight.X - mapMostLeft.X);
            var deltaMapY = Math.Abs(mapMostRight.Y - mapMostLeft.Y);

            var latsPerPixel = deltaLat / deltaMapX;
            var lonsPerPixel = deltaLon / deltaMapY;

            var leftOne = MapToMap(worldMostLeft, latsPerPixel, lonsPerPixel, worldMostLeft, mapMostLeft);
            var rightOne = MapToMap(worldMostRight, latsPerPixel, lonsPerPixel, worldMostLeft, mapMostLeft);

            var latMiddle = (Math.Abs(worldMostLeft.Latitude - worldMostRight.Latitude) / 2);
            var lonMiddle = (Math.Abs(worldMostLeft.Longitude - worldMostRight.Longitude) / 2);

            var middle = MapToMap(
                new TrackPoint(
                    worldMostLeft.Latitude + latMiddle, 
                    worldMostLeft.Longitude + lonMiddle), 
                latsPerPixel, lonsPerPixel, worldMostLeft, mapMostLeft);

            //S11.66472� E166.94862�
            var actualMiddle = MapToMap(
                new TrackPoint(
                    -11.66472,
                    166.94862), 
                latsPerPixel, lonsPerPixel, worldMostLeft, mapMostLeft);
            //S11.66425� E166.94722�
            var actualMiddle2 = MapToMap(
                new TrackPoint(
                    -11.66048, 
                    166.95450), 
                latsPerPixel, lonsPerPixel, worldMostLeft, mapMostLeft);
            
            Map.MapObjects.Add(new Point("map right", rightOne.X, rightOne.Y, 10, "#000000"));
            Map.MapObjects.Add(new Point("map left", leftOne.X, leftOne.Y, 10, "#000000"));
            Map.MapObjects.Add(new Point("middle", middle.X, middle.Y, 10, "#000000"));
            Map.MapObjects.Add(new Point("actual middle", actualMiddle.X, actualMiddle.Y, 10, "#FF0000"));
            Map.MapObjects.Add(new Point("actual middle 2", actualMiddle2.X, actualMiddle2.Y, 10, "#FF0000"));
            Map.MapObjects.Add(new Path("left to right", new [] { leftOne, middle, rightOne }, "#000000", 10));
        }

        private SKPoint MapToMap(TrackPoint point,
            double latsPerPixel,
            double lonsPerPixel,
            TrackPoint worldMostLeft,
            Avalonia.Point mapMostLeft)
        {
            var deltaLat = point.Latitude - worldMostLeft.Latitude;
            var deltaLon = point.Longitude - worldMostLeft.Longitude;

            var x = deltaLat / latsPerPixel;
            var y = deltaLon / lonsPerPixel;

            return new SKPoint((float)(mapMostLeft.X + x), (float)(mapMostLeft.Y - y));
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            float.TryParse(ZoomLevel.Text, out var zoomLevel);
            float.TryParse(ZoomX.Text, out var zoomX);
            float.TryParse(ZoomY.Text, out var zoomY);

            Map.Zoom(zoomLevel, new Avalonia.Point(zoomX, zoomY));
        }

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is MapObject x)
                {
                    Map.ZoomExtent(x.Name);
                }
            }
        }

        private void RadioButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var value = (sender as RadioButton)?.Content as string ?? "";

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

        private void Map_OnMapObjectSelected(object? sender, MapObjectSelectedEventArgs e)
        {
            Title = $"Selected {e.MapObject.Name}";
        }

        private void Map_OnDiagnosticsCaptured(object? sender, MapDiagnosticsEventArgs e)
        {
            Debug.WriteLine($"Map bounds: {Map.Bounds}, Map ViewportBounds: {e.ViewportBounds}, window bounds: {Bounds}");
        }

        private void WindowBase_OnActivated(object? sender, EventArgs e)
        {
            Squares1000();
        }
    }
}

