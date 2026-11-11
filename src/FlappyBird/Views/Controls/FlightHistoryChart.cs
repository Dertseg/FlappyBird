namespace FlappyBird.Views.Controls;

public sealed class FlightHistoryChart : GraphicsView, IDrawable
{
    private static readonly Color Accent = Color.FromArgb("#087F73");
    private static readonly Color GridLine = Color.FromArgb("#E1E8EF");
    private static readonly Color AxisText = Color.FromArgb("#526577");

    public static readonly BindableProperty ScoresProperty = BindableProperty.Create(nameof(Scores),
        typeof(IReadOnlyList<int>), typeof(FlightHistoryChart), Array.Empty<int>(),
        propertyChanged: (view, _, _) => ((FlightHistoryChart)view).Invalidate());

    public IReadOnlyList<int> Scores
    {
        get => (IReadOnlyList<int>)GetValue(ScoresProperty);
        set => SetValue(ScoresProperty, value);
    }

    public FlightHistoryChart() => Drawable = this;

    public void Draw(ICanvas canvas, RectF bounds)
    {
        if (Scores.Count == 0 || bounds.Width < 65 || bounds.Height < 35) return;

        const float left = 44;
        const float top = 10;
        float right = bounds.Width - 8;
        float bottom = bounds.Height - 20;
        int max = Math.Max(1, Scores.Max());
        canvas.StrokeColor = GridLine;
        canvas.StrokeSize = 1;
        canvas.DrawLine(left, top, right, top);
        canvas.DrawLine(left, bottom, right, bottom);
        canvas.FontColor = AxisText;
        canvas.FontSize = 11;
        canvas.DrawString(max.ToString(), left - 7, top + 4, HorizontalAlignment.Right);
        canvas.DrawString("0", left - 7, bottom + 4, HorizontalAlignment.Right);

        canvas.StrokeColor = Accent;
        canvas.StrokeSize = 2;
        canvas.FillColor = Accent;
        PointF previous = default;
        for (int i = 0; i < Scores.Count; i++)
        {
            var point = new PointF(Scores.Count == 1 ? (left + right) / 2 : left + (right - left) * i / (Scores.Count - 1),
                bottom - (bottom - top) * Scores[i] / max);
            if (i > 0) canvas.DrawLine(previous, point);
            canvas.FillCircle(point.X, point.Y, 3);
            previous = point;
        }
    }
}
