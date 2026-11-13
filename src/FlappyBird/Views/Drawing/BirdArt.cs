using FlappyBird.Models;

namespace FlappyBird.Views.Drawing;

internal sealed class BirdArt(BirdSkin skin)
{
    private readonly Color body = Color.FromArgb(skin.Body);
    private readonly Color wing = Color.FromArgb(skin.Wing);
    private readonly Color shade = Color.FromArgb(skin.Shade);
    private static readonly Color Eye = Color.FromArgb("#102A43");
    private static readonly Color Beak = Color.FromArgb("#EB7950");

    public void Draw(ICanvas canvas, float wingOffset = 0)
    {
        canvas.FillColor = shade;
        float radius = (float)GameEngine.BirdRadius;
        canvas.FillEllipse(-radius, -radius, 2 * radius, 2 * radius);
        canvas.FillColor = body;
        canvas.FillEllipse(-16, -16, 31, 29);
        canvas.FillColor = wing;
        canvas.FillEllipse(-15, wingOffset - 2, 18, 10);
        canvas.FillColor = Colors.White;
        canvas.FillEllipse(2, -12, 12, 14);
        canvas.FillColor = Eye;
        canvas.FillEllipse(8, -9, 5, 7);
        canvas.FillColor = Beak;
        canvas.FillRoundedRectangle(10, 0, 8, 6, 3);
    }
}
