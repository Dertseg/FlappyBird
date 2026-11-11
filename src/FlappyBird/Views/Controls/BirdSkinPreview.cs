using FlappyBird.Models;
using FlappyBird.Views.Drawing;

namespace FlappyBird.Views.Controls;

public sealed class BirdSkinPreview : GraphicsView, IDrawable
{
    public static readonly BindableProperty SkinProperty = BindableProperty.Create(nameof(Skin),
        typeof(BirdSkin), typeof(BirdSkinPreview), BirdSkin.All[0],
        propertyChanged: (view, _, value) => ((BirdSkinPreview)view).SetArt((BirdSkin?)value ?? BirdSkin.All[0]));

    private BirdArt art = new(BirdSkin.All[0]);

    public BirdSkin Skin
    {
        get => (BirdSkin)GetValue(SkinProperty);
        set => SetValue(SkinProperty, value);
    }

    public BirdSkinPreview() => Drawable = this;

    private void SetArt(BirdSkin skin)
    {
        art = new BirdArt(skin);
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF bounds)
    {
        float scale = Math.Min(bounds.Width / 50, bounds.Height / 40);
        canvas.SaveState();
        canvas.Translate(bounds.Center.X - 5 * scale, bounds.Center.Y);
        canvas.Scale(scale, scale);
        art.Draw(canvas);
        canvas.RestoreState();
    }
}
