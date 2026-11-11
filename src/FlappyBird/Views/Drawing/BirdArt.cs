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
        canvas.FillEllipse(-17, -16, 36, 34);
        canvas.FillColor = body;
        canvas.FillEllipse(-17, -18, 34, 32);
        canvas.FillColor = wing;
        canvas.FillEllipse(-18, wingOffset, 20, 12);
        canvas.FillColor = Colors.White;
        canvas.FillEllipse(3, -13, 13, 15);
        canvas.FillColor = Eye;
        canvas.FillEllipse(10, -9, 5, 7);
        canvas.FillColor = Beak;
        canvas.FillRoundedRectangle(12, 0, 16, 8, 3);
    }
}
