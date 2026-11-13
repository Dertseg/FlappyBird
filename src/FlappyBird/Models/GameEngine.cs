namespace FlappyBird.Models;

public enum GameState { Ready, Playing, Paused, GameOver }

public sealed class Pipe
{
    public double X { get; internal set; }
    public double GapY { get; init; }
    public bool Counted { get; internal set; }
}

/// <summary>Fixed 120 Hz simulation in a 360 x 640 world; no UI or database dependencies.</summary>
public sealed class GameEngine
{
    public const double Width = 360, Height = 640, GroundY = 590;
    public const double BirdX = 100, BirdRadius = 16, PipeWidth = 62, Gap = 166;
    public const double PipeRimOverhang = 4, PipeRimHeight = 18;
    public const double Step = 1.0 / 120, Speed = 130, Gravity = 950, FlapSpeed = -315;
    private readonly Random random;
    private readonly List<Pipe> pipes = [];
    private double accumulator;
    public GameEngine(int? seed = null) { random = seed.HasValue ? new(seed.Value) : new(); Reset(); }
    public GameState State { get; private set; }
    public double BirdY { get; private set; }
    public double Velocity { get; private set; }
    public int Score { get; private set; }
    public double Elapsed { get; private set; }
    public Guid SessionId { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public double PipeSpeed => DifficultyCatalog.Get(Difficulty).Speed;
    public double GapSize => DifficultyCatalog.Get(Difficulty).Gap;
    public IReadOnlyList<Pipe> Pipes => pipes;

    public void Reset(Difficulty difficulty = Difficulty.Normal)
    {
        _ = DifficultyCatalog.Get(difficulty);
        Difficulty = difficulty;
        SessionId = Guid.NewGuid(); State = GameState.Ready;
        BirdY = 280; Velocity = 0; Score = 0; Elapsed = 0; accumulator = 0;
        pipes.Clear();
        pipes.Add(new Pipe { X = 420, GapY = 280 });
        pipes.Add(new Pipe { X = 640, GapY = NextGap(280) });
    }
    public void Flap()
    {
        if (State == GameState.Ready) State = GameState.Playing;
        if (State == GameState.Playing) Velocity = FlapSpeed;
    }
    public void Pause() { if (State == GameState.Playing) { State = GameState.Paused; accumulator = 0; } }
    public void Resume() { if (State == GameState.Paused) State = GameState.Playing; }
    public void Finish() { if (State is GameState.Playing or GameState.Paused) State = GameState.GameOver; }
    public void Advance(double seconds)
    {
        if (State != GameState.Playing || !double.IsFinite(seconds) || seconds <= 0) return;
        // Discard long stalls rather than moving the bird through an entire obstacle.
        accumulator += Math.Min(seconds, 0.1);
        while (accumulator >= Step && State == GameState.Playing)
        {
            accumulator -= Step; Tick();
        }
    }
    private void Tick()
    {
        Elapsed += Step; Velocity += Gravity * Step; BirdY += Velocity * Step;
        foreach (var pipe in pipes) pipe.X -= PipeSpeed * Step;
        if (BirdY - BirdRadius <= 0 || BirdY + BirdRadius >= GroundY || pipes.Any(Hits))
        {
            State = GameState.GameOver; return;
        }
        foreach (var pipe in pipes)
            if (!pipe.Counted && pipe.X + PipeWidth + PipeRimOverhang < BirdX - BirdRadius) { pipe.Counted = true; Score++; }
        if (pipes[0].X + PipeWidth < 0)
        {
            pipes.RemoveAt(0);
            var last = pipes[^1];
            pipes.Add(new Pipe { X = last.X + 220, GapY = NextGap(last.GapY) });
        }
    }
    private double NextGap(double previous) => Math.Clamp(previous + random.Next(-70, 71), 175, 410);
    private bool Hits(Pipe p)
    {
        double top = p.GapY - GapSize / 2, bottom = p.GapY + GapSize / 2;
        return CircleRect(p.X, 0, PipeWidth, top)
            || CircleRect(p.X, bottom, PipeWidth, GroundY - bottom)
            || CircleRect(p.X - PipeRimOverhang, top - PipeRimHeight, PipeWidth + 2 * PipeRimOverhang, PipeRimHeight)
            || CircleRect(p.X - PipeRimOverhang, bottom, PipeWidth + 2 * PipeRimOverhang, PipeRimHeight);
    }
    private bool CircleRect(double x, double y, double w, double h)
    {
        double dx = BirdX - Math.Clamp(BirdX, x, x + w), dy = BirdY - Math.Clamp(BirdY, y, y + h);
        return dx * dx + dy * dy <= BirdRadius * BirdRadius;
    }
}
