using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace lab4_task3
{
    public partial class VisualizationWindow : Window
    {
        private Point _lastMousePosition;
        private bool _isDragging = false;

        public VisualizationWindow()
        {
            InitializeComponent();
        }

        public void LoadPlotData(LandPlotModel plot)
        {
            TxtOwner.Text = plot.OwnerName;
            TxtLocation.Text = plot.Location;
            TxtPurpose.Text = plot.Pryznachennya;
            TxtSoil.Text = plot.SoilType;
            TxtPrice.Text = plot.MarketValueFormatted;

            DrawPolygon(plot.Coordinates);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.GoBack(this);
        }

        private void DrawPolygon(List<Point> coordinates)
        {
            if (coordinates == null || coordinates.Count < 3) return;

            MapCanvas.Children.Clear();

            Polygon plot = new Polygon
            {
                Stroke = (Brush)FindResource("DarkGreenBrush"),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Color.FromArgb(120, 167, 189, 64)),
                Points = new PointCollection(coordinates)
            };
            MapCanvas.Children.Add(plot);

            foreach (var point in coordinates)
            {
                Ellipse dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = (Brush)FindResource("ExitBrush")
                };
                Canvas.SetLeft(dot, point.X - 4);
                Canvas.SetTop(dot, point.Y - 4);
                MapCanvas.Children.Add(dot);

                TextBlock textBlock = new TextBlock
                {
                    Text = $"({point.X}; {point.Y})",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("DarkGreenBrush"),
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
                    Padding = new Thickness(3, 1, 3, 1)
                };

                Canvas.SetLeft(textBlock, point.X + 6);
                Canvas.SetTop(textBlock, point.Y - 20);
                MapCanvas.Children.Add(textBlock);
            }
        }

        private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = 1.1;
            if (e.Delta < 0) zoomFactor = 1.0 / zoomFactor;

            if (MapScale.ScaleX * zoomFactor < 0.2 || MapScale.ScaleX * zoomFactor > 10) return;

            Point mousePos = e.GetPosition(MapCanvas);

            MapTranslate.X -= mousePos.X * (zoomFactor - 1) * MapScale.ScaleX;
            MapTranslate.Y -= mousePos.Y * (zoomFactor - 1) * MapScale.ScaleY;

            MapScale.ScaleX *= zoomFactor;
            MapScale.ScaleY *= zoomFactor;
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(this);
            MapCanvas.CaptureMouse();
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            MapCanvas.ReleaseMouseCapture();
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);

                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;

                MapTranslate.X += deltaX;
                MapTranslate.Y += deltaY;

                _lastMousePosition = currentPosition;
            }
        }

        private void BtnResetMap_Click(object sender, RoutedEventArgs e)
        {
            MapScale.ScaleX = 1;
            MapScale.ScaleY = 1;
            MapTranslate.X = 0;
            MapTranslate.Y = 0;
        }
    }
}