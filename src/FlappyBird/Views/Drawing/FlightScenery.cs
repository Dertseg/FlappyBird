using FlappyBird.Models;

namespace FlappyBird.Views.Drawing;

internal sealed class FlightScenery
{
    private static readonly FlightScenery Meadow = new("#C7E9EC", "#EAF9F4", "#C7E9EC", "#A9C791", "#76A875", "#DCE3BD", 0);
    private static readonly FlightScenery Coast = new("#C9E5F0", "#EFF7F5", "#A1CADB", "#DCC598", "#CBB184", "#EFE1BA", 1);
    private static readonly FlightScenery Peaks = new("#D9E5ED", "#F3F6F8", "#B1C1D0", "#ACB8AA", "#889C89", "#CFD6C7", 2);
    private static readonly FlightScenery Dusk = new("#B7ACC9", "#EAD7DF", "#9A8CB1", "#9E9CB2", "#7F809D", "#C0B9CA", 3);
    private static readonly PathF Mountains = new PathF()
        .MoveTo(0, 1).LineTo(0, .73f).LineTo(.22f, .38f).LineTo(.42f, .74f)
        .LineTo(.65f, .29f).LineTo(1, .82f).LineTo(1, 1).LineTo(0, 1);

    private readonly Color detail;
    private readonly Color backdrop;
    private readonly Color ground;
    private readonly Color grass;
    private readonly Color groundMark;
    private readonly int scene;
    public Color Sky { get; }

    private FlightScenery(string sky, string detail, string backdrop, string ground, string grass, string groundMark, int scene)
    {
        Sky = Color.FromArgb(sky);
        this.detail = Color.FromArgb(detail);
        this.backdrop = Color.FromArgb(backdrop);
        this.ground = Color.FromArgb(ground);
        this.grass = Color.FromArgb(grass);
        this.groundMark = Color.FromArgb(groundMark);
        this.scene = scene;
    }

    public static FlightScenery For(FlightLocation location) => location.Id switch
    {
        "coast" => Coast,
        "peaks" => Peaks,
        "dusk" => Dusk,
        _ => Meadow
    };

    public void DrawSky(ICanvas canvas, float width, float height, double elapsed)
    {
        canvas.FillColor = detail;
        if (scene == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = (float)((i * 130 + 440 - elapsed * 12 % 520 + 520) % 520 - 80) * width / 360;
                float y = (.16f + i % 2 * .11f) * height;
                canvas.FillRoundedRectangle(x, y, .25f * width, .04f * height, .02f * height);
                canvas.FillEllipse(x + .055f * width, y - .025f * height, .11f * width, .065f * height);
            }
            return;
        }

        float sunSize = Math.Min(.14f * width, .12f * height);
        canvas.FillEllipse(.72f * width, .15f * height, sunSize, sunSize);
        canvas.FillColor = backdrop;
        if (scene == 1)
        {
            canvas.FillRectangle(0, .68f * height, width, .32f * height);
            canvas.StrokeColor = detail;
            canvas.StrokeSize = Math.Max(1, height / 350);
            canvas.DrawLine(.08f * width, .78f * height, .37f * width, .78f * height);
            canvas.DrawLine(.59f * width, .86f * height, .87f * width, .86f * height);
        }
        else if (scene == 2)
        {
            canvas.SaveState();
            canvas.Scale(width, height);
            canvas.FillPath(Mountains);
            canvas.RestoreState();
        }
        else
        {
            canvas.FillEllipse(-.22f * width, .68f * height, .85f * width, .6f * height);
            canvas.FillEllipse(.35f * width, .76f * height, .86f * width, .52f * height);
        }
    }

    public void DrawGround(ICanvas canvas, double offset)
    {
        canvas.FillColor = ground;
        canvas.FillRectangle(0, 590, 360, 50);
        canvas.FillColor = grass;
        canvas.FillRectangle(0, 590, 360, 7);
        canvas.FillColor = groundMark;
        for (int i = 0; i < 17; i++)
            canvas.FillRectangle((float)(i * 24 - offset % 24), 610, 12, 4);
    }
}
