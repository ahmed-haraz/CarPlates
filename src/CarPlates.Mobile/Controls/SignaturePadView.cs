using System.Collections.ObjectModel;
using System.Globalization;
using CarPlates.Mobile.Helpers;
using Microsoft.Maui.Graphics;

namespace CarPlates.Mobile.Controls;

public class SignaturePadView : GraphicsView, IDrawable
{
    private readonly ObservableCollection<List<PointF>> _strokes = new();
    private List<PointF>? _activeStroke;

    public static readonly BindableProperty SignatureDataProperty = BindableProperty.Create(
        nameof(SignatureData), typeof(string), typeof(SignaturePadView), null,
        BindingMode.TwoWay, propertyChanged: OnSignatureDataChanged);

    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly), typeof(bool), typeof(SignaturePadView), false);

    public string? SignatureData
    {
        get => (string?)GetValue(SignatureDataProperty);
        set => SetValue(SignatureDataProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public SignaturePadView()
    {
        Drawable = this;
        BackgroundColor = Colors.White;
        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
        EndInteraction += OnEndInteraction;
        SizeChanged += (_, _) =>
        {
            if (Width > 0 && Height > 0 && !string.IsNullOrWhiteSpace(SignatureData))
                LoadSignature(SignatureData);
        };
    }

    public void Clear()
    {
        _strokes.Clear();
        SignatureData = string.Empty;
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        foreach (var stroke in _strokes.Where(s => s.Count > 0))
        {
            var path = new PathF();
            path.MoveTo(stroke[0]);
            foreach (var point in stroke.Skip(1))
            {
                path.LineTo(point);
            }
            canvas.DrawPath(path);
        }
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (IsReadOnly) return;
        _activeStroke = new List<PointF> { e.Touches[0] };
        _strokes.Add(_activeStroke);
        Invalidate();
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (IsReadOnly || _activeStroke == null) return;
        _activeStroke.Add(e.Touches[0]);
        Invalidate();
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (IsReadOnly) return;
        SignatureData = Serialize();
        _activeStroke = null;
    }

    private string Serialize()
    {
        float maxX = 0, maxY = 0;
        foreach (var stroke in _strokes)
        {
            foreach (var p in stroke)
            {
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
        }

        float width = Math.Max(maxX, 1);
        float height = Math.Max(maxY, 1);
        return string.Join('|', _strokes.Select(stroke => string.Join(';', stroke.Select(point =>
            string.Create(CultureInfo.InvariantCulture, $"{point.X / width:0.##},{point.Y / height:0.##}")))));
    }

    private void LoadSignature(string? data)
    {
        _strokes.Clear();
        if (!string.IsNullOrWhiteSpace(data))
        {
            foreach (var strokeData in data.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var stroke = new List<PointF>();
                foreach (var pointData in strokeData.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pointData.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2
                        && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var nx)
                        && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var ny))
                    {
                        stroke.Add(ConversionHelpers.ToPointF(nx * Width, ny * Height));
                    }
                }
                if (stroke.Count > 0) _strokes.Add(stroke);
            }
        }
        Invalidate();
    }

    private static void OnSignatureDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (SignaturePadView)bindable;
        if (newValue is string data && data != view.Serialize())
        {
            view.LoadSignature(data);
        }
    }
}
