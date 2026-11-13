using Android.Media;
namespace FlappyBird.Services;
public sealed class AndroidFeedback : IGameFeedback, IDisposable
{
    private readonly Lazy<SoundPool> pool;
    private readonly Dictionary<string, int> sounds = [];
    public AndroidFeedback() => pool = new Lazy<SoundPool>(CreatePool);

    private SoundPool CreatePool()
    {
        using var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Sonification)!.Build();
        var player = new SoundPool.Builder().SetMaxStreams(3)!.SetAudioAttributes(attributes)!.Build()!;
        foreach (var name in new[] { "flap", "point", "hit" })
        {
            try { using var asset = Android.App.Application.Context.Assets!.OpenFd(name + ".wav"); sounds[name] = player.Load(asset, 1); }
            catch (Java.IO.IOException) { /* Optional feedback must not block gameplay. */ }
        }
        return player;
    }
    public void WarmUp() => _ = pool.Value;
    public void Play(string sound) { var player = pool.Value; if (sounds.TryGetValue(sound, out var id)) player.Play(id, 0.35f, 0.35f, 1, 0, 1); }
    public void Vibrate() { try { if (Vibration.Default.IsSupported) Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(70)); } catch (FeatureNotSupportedException) { } }
    public void Dispose() { if (pool.IsValueCreated) { pool.Value.Release(); pool.Value.Dispose(); } }
}
