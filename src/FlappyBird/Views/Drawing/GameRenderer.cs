using FlappyBird.Models;
namespace FlappyBird.Views.Drawing;
public sealed class GameRenderer(GameEngine game, BirdSkin skin, FlightLocation location) : IDrawable
{
    private readonly BirdArt bird = new(skin);
    private readonly FlightScenery scenery = FlightScenery.For(location);
    private static readonly Color Pipe = Color.FromArgb("#116466");
    private static readonly Color PipeHighlight = Color.FromArgb("#258B81");
    private static readonly Color PipeRim = Color.FromArgb("#0D4C50");

    public void Draw(ICanvas c, RectF bounds)
    {
        c.FillColor = scenery.Sky; c.FillRectangle(bounds);
        float scale = Math.Min(bounds.Width / 360, bounds.Height / 640);
        c.SaveState(); c.Translate((bounds.Width - 360 * scale) / 2, (bounds.Height - 640 * scale) / 2); c.Scale(scale, scale);
        c.ClipRectangle(0, 0, 360, 640);
        scenery.DrawSky(c, 360, 590, game.Elapsed);
        const float pipeWidth = (float)GameEngine.PipeWidth;
        const float overhang = (float)GameEngine.PipeRimOverhang;
        const float rimHeight = (float)GameEngine.PipeRimHeight;
        foreach (var p in game.Pipes)
        {
            float x = (float)p.X, top = (float)(p.GapY - game.GapSize / 2), bottom = (float)(p.GapY + game.GapSize / 2);
            c.FillColor = Pipe; c.FillRectangle(x, 0, pipeWidth, top); c.FillRectangle(x, bottom, pipeWidth, 590 - bottom);
            c.FillColor = PipeHighlight; c.FillRectangle(x + 8, 0, 10, Math.Max(0, top - 12)); c.FillRectangle(x + 8, bottom + 12, 10, 590 - bottom);
            c.FillColor = PipeRim; c.FillRectangle(x - overhang, top - rimHeight, pipeWidth + 2 * overhang, rimHeight);
            c.FillRectangle(x - overhang, bottom, pipeWidth + 2 * overhang, rimHeight);
        }
        scenery.DrawGround(c, game.Elapsed * game.PipeSpeed);
        c.SaveState(); c.Translate((float)GameEngine.BirdX, (float)game.BirdY); c.Rotate((float)Math.Clamp(game.Velocity * 0.09, -22, 65));
        bird.Draw(c, (float)(Math.Sin(game.Elapsed * 24) * 3));
        c.RestoreState(); c.RestoreState();
    }
}
