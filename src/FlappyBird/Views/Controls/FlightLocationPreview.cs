using FlappyBird.Models;
using FlappyBird.Views.Drawing;

namespace FlappyBird.Views.Controls;

public sealed class FlightLocationPreview : GraphicsView, IDrawable
{
    public static readonly BindableProperty LocationProperty = BindableProperty.Create(
        nameof(Location), typeof(FlightLocation), typeof(FlightLocationPreview), FlightLocation.All[0],
        propertyChanged: (view, _, _) => ((FlightLocationPreview)view).Invalidate());

    public FlightLocation Location
    {
        get => (FlightLocation)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    public FlightLocationPreview() => Drawable = this;

    public void Draw(ICanvas canvas, RectF bounds)
    {
        var scenery = FlightScenery.For(Location);
        canvas.SaveState();
        canvas.ClipRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        canvas.FillColor = scenery.Sky;
        canvas.FillRectangle(bounds);
        scenery.DrawSky(canvas, bounds.Width, bounds.Height, 0);
        canvas.RestoreState();
    }
}
