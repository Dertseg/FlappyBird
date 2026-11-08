using Android.Media;
using FlappyBird.Core;
namespace FlappyBird.Services;
public sealed class AndroidFeedback : IGameFeedback, IDisposable
{
    private readonly SoundPool pool;
    private readonly Dictionary<string, int> sounds = [];
    public AndroidFeedback()
    {
        using var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Sonification)!.Build();
        pool = new SoundPool.Builder().SetMaxStreams(3)!.SetAudioAttributes(attributes)!.Build()!;
        foreach (var name in new[] { "flap", "point", "hit" })
        {
            try { using var asset = Android.App.Application.Context.Assets!.OpenFd(name + ".wav"); sounds[name] = pool.Load(asset, 1); }
            catch (Java.IO.IOException) { /* Optional feedback must not block gameplay. */ }
        }
    }
    public void Play(string sound) { if (sounds.TryGetValue(sound, out var id)) pool.Play(id, 0.35f, 0.35f, 1, 0, 1); }
    public void Vibrate() { try { if (Vibration.Default.IsSupported) Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(70)); } catch (FeatureNotSupportedException) { } }
    public void Dispose() { pool.Release(); pool.Dispose(); }
}
