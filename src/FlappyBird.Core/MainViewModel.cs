using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlappyBird.Core;

public enum Screen { Menu, Game, Leaderboard, Settings }
public sealed record ScoreRow(int Place, string Player, int Score, string Date);

public partial class MainViewModel : ObservableObject
{
    private readonly IGameStore store;
    private readonly IGameFeedback feedback;
    private GameResult? pendingResult;
    private CancellationTokenSource? countdown;
    private bool initialized;
    public GameEngine Engine { get; } = new();
    public event Action? FrameChanged;
    [ObservableProperty] private Screen currentScreen = Screen.Menu;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private int bestScore;
    [ObservableProperty] private int score;
    [ObservableProperty] private string playerName = "Игрок";
    [ObservableProperty] private bool soundEnabled = true;
    [ObservableProperty] private bool vibrationEnabled = true;
    [ObservableProperty] private bool isReady;
    [ObservableProperty] private bool isPaused;
    [ObservableProperty] private bool isGameOver;
    [ObservableProperty] private bool canRetrySave;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool canRetryInitialization;
    [ObservableProperty] private string countdownText = "";
    [ObservableProperty] private string resultTitle = "Попытка завершена";
    [ObservableProperty] private string medalText = "";
    [ObservableProperty] private IReadOnlyList<ScoreRow> leaders = [];
    public bool IsMenu => CurrentScreen == Screen.Menu;
    public bool IsGame => CurrentScreen == Screen.Game;
    public bool IsLeaderboard => CurrentScreen == Screen.Leaderboard;
    public bool IsSettings => CurrentScreen == Screen.Settings;
    public bool HasCountdown => CountdownText.Length > 0;

    public MainViewModel(IGameStore store, IGameFeedback feedback) { this.store = store; this.feedback = feedback; }
    partial void OnCurrentScreenChanged(Screen value)
    {
        OnPropertyChanged(nameof(IsMenu)); OnPropertyChanged(nameof(IsGame));
        OnPropertyChanged(nameof(IsLeaderboard)); OnPropertyChanged(nameof(IsSettings));
    }
    partial void OnCountdownTextChanged(string value) => OnPropertyChanged(nameof(HasCountdown));

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy || initialized) return;
        IsBusy = true;
        try
        {
            await store.InitializeAsync();
            var settings = await store.GetSettingsAsync();
            PlayerName = settings.PlayerName; SoundEnabled = settings.SoundEnabled; VibrationEnabled = settings.VibrationEnabled;
            await RefreshBestAsync(); initialized = true; CanRetryInitialization = false; Status = "";
        }
        catch { CanRetryInitialization = true; Status = "Не удалось открыть базу. Проверьте свободное место и нажмите «Повторить»."; }
        finally { IsBusy = false; }
    }
    private async Task RefreshBestAsync()
    {
        var results = await store.GetTopAsync(); BestScore = results.FirstOrDefault()?.Score ?? 0;
        Leaders = results.Select((r, i) => new ScoreRow(i + 1, r.PlayerName, r.Score, r.PlayedAtUtc.ToLocalTime().ToString("dd.MM.yy HH:mm"))).ToArray();
    }
    [RelayCommand]
    private async Task StartAsync()
    {
        if (!initialized) await InitializeAsync();
        if (!initialized || IsBusy) return;
        if (pendingResult is not null) { Status = "Сначала сохраните результат или явно откажитесь от сохранения."; return; }
        CancelCountdown(); Engine.Reset(); Score = 0; IsReady = true; IsPaused = false; IsGameOver = false;
        CanRetrySave = false; MedalText = ""; Status = ""; CurrentScreen = Screen.Game; FrameChanged?.Invoke();
    }
    [RelayCommand]
    private void Flap()
    {
        if (!IsGame || HasCountdown) return;
        if (Engine.State is not (GameState.Ready or GameState.Playing)) return;
        Engine.Flap(); IsReady = false; if (SoundEnabled) feedback.Play("flap");
    }
    public void Advance(double seconds)
    {
        if (!IsGame || Engine.State != GameState.Playing) return;
        int before = Engine.Score; Engine.Advance(seconds); Score = Engine.Score;
        if (before != Score && SoundEnabled) feedback.Play("point");
        FrameChanged?.Invoke();
        if (Engine.State == GameState.GameOver && !IsGameOver)
        {
            IsGameOver = true;
            if (SoundEnabled) feedback.Play("hit"); if (VibrationEnabled) feedback.Vibrate();
            ResultTitle = Score > BestScore ? "Новый рекорд!" : "Попытка завершена";
            MedalText = Score switch { >= 100 => "Платиновая медаль", >= 50 => "Золотая медаль", >= 25 => "Серебряная медаль", >= 10 => "Бронзовая медаль", _ => "До первой медали — 10 очков" };
            pendingResult = new GameResult { Id = Engine.SessionId, PlayerName = CleanName(), Score = Score, DurationSeconds = Engine.Elapsed, PlayedAtUtc = DateTime.UtcNow };
            SaveResultCommand.Execute(null);
        }
    }
    [RelayCommand]
    private void Pause() { CancelCountdown(); Engine.Pause(); IsPaused = Engine.State == GameState.Paused; }
    public void OnBackgrounded() => Pause();
    [RelayCommand]
    private async Task ResumeAsync()
    {
        if (!IsPaused || countdown is not null) return;
        using var source = new CancellationTokenSource(); countdown = source;
        try
        {
            for (int i = 3; i > 0; i--) { CountdownText = i.ToString(); await Task.Delay(700, source.Token); }
            Engine.Resume(); IsPaused = false;
        }
        catch (OperationCanceledException) { }
        finally { if (ReferenceEquals(countdown, source)) countdown = null; CountdownText = ""; }
    }
    private void CancelCountdown() { countdown?.Cancel(); CountdownText = ""; }
    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (pendingResult is null || IsBusy) return;
        IsBusy = true; CanRetrySave = false;
        try
        {
            await store.SaveResultAsync(pendingResult); pendingResult = null;
            Status = "Результат сохранён";
            try { await RefreshBestAsync(); } catch { Status = "Результат сохранён. Таблицу можно обновить позже."; }
        }
        catch { CanRetrySave = true; Status = "Не удалось сохранить результат. Повторите или выберите «Не сохранять»."; }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private void DiscardResult() { if (IsBusy) return; pendingResult = null; CanRetrySave = false; Status = "Результат не сохранён"; }
    [RelayCommand]
    private void Menu()
    {
        if (IsBusy) return;
        Pause(); CurrentScreen = Screen.Menu; Status = pendingResult is null ? "" : "Есть несохранённая попытка. Вернитесь к результату.";
    }
    [RelayCommand]
    private void ReturnToResult() => CurrentScreen = Screen.Game;
    [RelayCommand]
    private async Task LeaderboardAsync()
    {
        if (IsBusy) return;
        if (!initialized) await InitializeAsync();
        if (!initialized) return;
        IsBusy = true; CurrentScreen = Screen.Leaderboard;
        try { await RefreshBestAsync(); Status = Leaders.Count == 0 ? "Пока нет результатов. Сыграйте первую попытку!" : ""; }
        catch { Status = "Не удалось загрузить рекорды. Нажмите «Обновить»."; }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private void Settings() { CurrentScreen = Screen.Settings; Status = ""; }
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (IsBusy) return;
        if (!initialized) await InitializeAsync();
        if (!initialized) return;
        IsBusy = true;
        try
        {
            PlayerName = CleanName();
            await store.SaveSettingsAsync(new PlayerSettings { PlayerName = PlayerName, SoundEnabled = SoundEnabled, VibrationEnabled = VibrationEnabled });
            CurrentScreen = Screen.Menu; Status = "Настройки сохранены";
        }
        catch { Status = "Не удалось сохранить настройки. Повторите попытку."; }
        finally { IsBusy = false; }
    }
    private string CleanName() => string.IsNullOrWhiteSpace(PlayerName) ? "Игрок" : PlayerName.Trim()[..Math.Min(PlayerName.Trim().Length, 20)];
}
