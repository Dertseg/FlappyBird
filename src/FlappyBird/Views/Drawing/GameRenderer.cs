using FlappyBird.Models;
namespace FlappyBird.Views.Drawing;
public sealed class GameRenderer(GameEngine game) : IDrawable
{
    public void Draw(ICanvas c, RectF bounds)
    {
        c.FillColor = Color.FromArgb("#C7E9EC"); c.FillRectangle(bounds);
        float scale = Math.Min(bounds.Width / 360, bounds.Height / 640);
        c.SaveState(); c.Translate((bounds.Width - 360 * scale) / 2, (bounds.Height - 640 * scale) / 2); c.Scale(scale, scale);
        c.ClipRectangle(0, 0, 360, 640);
        c.FillColor = Color.FromArgb("#EAF9F4");
        for (int i = 0; i < 4; i++)
        {
            float x = (float)((i * 130 + 400 - game.Elapsed * 12 % 520) % 520 - 80);
            c.FillRoundedRectangle(x, 90 + i % 2 * 65, 90, 24, 12); c.FillEllipse(x + 20, 76 + i % 2 * 65, 40, 40);
        }
        foreach (var p in game.Pipes)
        {
            float x = (float)p.X, top = (float)(p.GapY - GameEngine.Gap / 2), bottom = (float)(p.GapY + GameEngine.Gap / 2);
            c.FillColor = Color.FromArgb("#116466"); c.FillRoundedRectangle(x, -20, 62, top + 20, 5); c.FillRoundedRectangle(x, bottom, 62, 600 - bottom, 5);
            c.FillColor = Color.FromArgb("#258B81"); c.FillRectangle(x + 8, 0, 10, Math.Max(0, top - 12)); c.FillRectangle(x + 8, bottom + 12, 10, 590 - bottom);
            c.FillColor = Color.FromArgb("#0D4C50"); c.FillRoundedRectangle(x - 4, top - 18, 70, 18, 4); c.FillRoundedRectangle(x - 4, bottom, 70, 18, 4);
        }
        c.FillColor = Color.FromArgb("#A9C791"); c.FillRectangle(0, 590, 360, 50);
        c.FillColor = Color.FromArgb("#76A875"); c.FillRectangle(0, 590, 360, 7);
        c.FillColor = Color.FromArgb("#DCE3BD");
        for (int i = 0; i < 20; i++) c.FillRectangle((float)(i * 24 - game.Elapsed * 130 % 24), 610, 12, 4);
        c.SaveState(); c.Translate(100, (float)game.BirdY); c.Rotate((float)Math.Clamp(game.Velocity * 0.09, -22, 65));
        c.FillColor = Color.FromArgb("#DAA53D"); c.FillEllipse(-17, -16, 36, 34);
        c.FillColor = Color.FromArgb("#FFD56A"); c.FillEllipse(-17, -18, 34, 32);
        c.FillColor = Color.FromArgb("#F4B544"); c.FillEllipse(-18, (float)(Math.Sin(game.Elapsed * 24) * 3), 20, 12);
        c.FillColor = Colors.White; c.FillEllipse(3, -13, 13, 15);
        c.FillColor = Color.FromArgb("#102A43"); c.FillEllipse(10, -9, 5, 7);
        c.FillColor = Color.FromArgb("#EB7950"); c.FillRoundedRectangle(12, 0, 16, 8, 3);
        c.RestoreState(); c.RestoreState();
    }
}
