using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public partial class HeatmapSurface3DView : UserControl
{
    private const double MinZoomFactor = 0.55;
    private const double MaxZoomFactor = 2.4;

    private Point _lastMouse;
    private bool _isDragging;
    private double _rotationX = 28;
    private double _rotationY = -38;
    private double _zoomFactor = 1;

    private readonly AxisAngleRotation3D _rotateX = new(new Vector3D(1, 0, 0), 28);
    private readonly AxisAngleRotation3D _rotateY = new(new Vector3D(0, 1, 0), -38);
    private readonly Transform3DGroup _surfaceTransform = new();

    public HeatmapSurface3DView()
    {
        InitializeComponent();
        _surfaceTransform.Children.Add(new RotateTransform3D(_rotateY));
        _surfaceTransform.Children.Add(new RotateTransform3D(_rotateX));
        SurfaceHost.Transform = _surfaceTransform;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        MouseLeave += (_, _) => _isDragging = false;
    }

    public void Render(HeatmapChartModel model, double colorMin, double colorMax)
    {
        if (model.XAxis.Length < 2 || model.YAxis.Length < 2)
        {
            SurfaceHost.Content = null;
            return;
        }

        var surface = HeatmapSurfaceMeshBuilder.BuildSurface(model, colorMin, colorMax, out var bounds);
        var center = new Point3D(
            bounds.X + bounds.SizeX / 2,
            bounds.Y + bounds.SizeY / 2,
            bounds.Z + bounds.SizeZ / 2);

        var group = new Model3DGroup { Children = { surface } };
        SurfaceHost.Content = group;

        var offset = new TranslateTransform3D(-center.X, -center.Y, -center.Z);
        _surfaceTransform.Children.Clear();
        _surfaceTransform.Children.Add(offset);
        _surfaceTransform.Children.Add(new RotateTransform3D(_rotateY));
        _surfaceTransform.Children.Add(new RotateTransform3D(_rotateX));
        _rotateX.Angle = _rotationX;
        _rotateY.Angle = _rotationY;
        UpdateCamera();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _lastMouse = e.GetPosition(this);
        CaptureMouse();
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
            return;

        var position = e.GetPosition(this);
        var delta = position - _lastMouse;
        _lastMouse = position;

        _rotationY += delta.X * 0.45;
        _rotationX = Math.Clamp(_rotationX + delta.Y * 0.45, -80, 80);
        _rotateX.Angle = _rotationX;
        _rotateY.Angle = _rotationY;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 1.08 : 1 / 1.08;
        _zoomFactor = Math.Clamp(_zoomFactor * delta, MinZoomFactor, MaxZoomFactor);
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var distance = 1.85 / _zoomFactor;
        Camera.Position = new Point3D(distance, distance * 0.78, distance);
        Camera.LookDirection = new Vector3D(-distance, -distance * 0.78, -distance);
    }
}
