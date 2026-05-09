using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using lab4_task3.DTO;

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

        public void LoadPlotData(Property property)
        {
            TxtOwner.Text = $"{property.Owner.LastName} {property.Owner.FirstName}";
            TxtLocation.Text = property.Locality.Title;
            TxtPurpose.Text = property.Usage;
            TxtSoil.Text = property.Description.Soil;
            TxtPrice.Text = $"{property.Price:F2} грн";

            List<Point> points = new List<Point>();
            foreach (var coord in property.Description.Coordinates)
            {
                points.Add(new Point(coord[0], coord[1]));
            }

            DrawPolygon(points);
            MapCanvas.Loaded += (s, e) => CenterPolygon(points);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppUtils.GoBack(this);

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

        private void CenterPolygon(List<Point> coordinates)
        {
            if (coordinates == null || coordinates.Count == 0) return;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var p in coordinates)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;

            double canvasCenterX = MapCanvas.ActualWidth / 2;
            double canvasCenterY = MapCanvas.ActualHeight / 2;

            MapTranslate.X = canvasCenterX - centerX;
            MapTranslate.Y = canvasCenterY - centerY;

            MapScale.ScaleX = 1;
            MapScale.ScaleY = 1;
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