using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlappyBird.Data;
using FlappyBird.Models;
using FlappyBird.Services;
namespace FlappyBird.ViewModels;

public partial class GameViewModel(IGameStore store, IGameFeedback feedback, INavigationService navigation) : BaseViewModel(store)
{
    private PlayerSettings settings = new();
    private GameResult? pendingResult;
    private CancellationTokenSource? countdown;
    public GameEngine Engine { get; } = new();
    public BirdSkin Skin => BirdSkin.Get(settings.BirdSkinId);
    public FlightLocation Location => FlightLocation.Get(settings.FlightLocationId);
    public event Action? FrameChanged;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady)), NotifyPropertyChangedFor(nameof(IsPlaying)),
        NotifyPropertyChangedFor(nameof(IsPaused)), NotifyPropertyChangedFor(nameof(IsGameOver)), NotifyPropertyChangedFor(nameof(ShowGameHeader))]
    private GameState phase = GameState.Ready;
    [ObservableProperty] private int score;
    [ObservableProperty] private int previousBest;
    [ObservableProperty] private int bestScore;
    [ObservableProperty] private string difficultyName = "Обычный";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DurationText))] private double durationSeconds;
    [ObservableProperty] private string resultTitle = "Хороший полёт!";
    [ObservableProperty] private string medalText = "";
    [ObservableProperty] private string nextMedalText = "";
    [ObservableProperty] private double medalFraction;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasNewAchievements))] private string newAchievementsText = "";
    [ObservableProperty] private bool canRetrySave;
    [ObservableProperty] private bool isChoosingExit;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCountdown))] private string countdownText = "";
    public bool IsReady => Phase == GameState.Ready;
    public bool IsPlaying => Phase == GameState.Playing;
    public bool IsPaused => Phase == GameState.Paused;
    public bool IsGameOver => Phase == GameState.GameOver;
    public bool ShowGameHeader => !IsGameOver;
    public bool HasCountdown => CountdownText.Length > 0;
    public bool HasNewAchievements => NewAchievementsText.Length > 0;
    public string DurationText => $"{(int)(DurationSeconds / 60):00}:{(int)DurationSeconds % 60:00}.{(int)(DurationSeconds * 10) % 10}";

    public async Task<bool> PrepareAsync(Difficulty difficulty)
    {
        if (pendingResult is not null) { Status = "Сначала сохрани результат или выбери «Не сохранять»."; return false; }
        return await TryAsync(async () =>
        {
            var loadedSettings = await Store.GetSettingsAsync();
            if (loadedSettings.SoundEnabled) _ = Task.Run(WarmUpAudio);
            int best = await Store.GetBestScoreAsync(difficulty);
            CancelCountdown(); settings = loadedSettings; Engine.Reset(difficulty);
            PreviousBest = best; BestScore = best; Score = 0; DurationSeconds = 0;
            DifficultyName = DifficultyCatalog.Get(difficulty).Name;
            CanRetrySave = false; IsChoosingExit = false; NewAchievementsText = ""; MedalText = ""; NextMedalText = "";
            Phase = GameState.Ready; FrameChanged?.Invoke();
        }, "Не удалось подготовить полёт. Вернись в меню и попробуй снова.");
    }
    [RelayCommand]
    private void Flap()
    {
        if (IsBusy || HasCountdown || Phase is not (GameState.Ready or GameState.Playing)) return;
        Engine.Flap(); Phase = Engine.State;
        if (settings.SoundEnabled) SafePlay("flap"); FrameChanged?.Invoke();
    }
    public void Advance(double seconds)
    {
        if (!IsPlaying) return;
        var before = Engine.Score; Engine.Advance(seconds); Score = Engine.Score;
        if (before != Score && settings.SoundEnabled) SafePlay("point");
        FrameChanged?.Invoke();
        if (Engine.State == GameState.GameOver) CompleteAttempt();
    }
    private void SafePlay(string sound)
    {
        try { feedback.Play(sound); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    private void WarmUpAudio()
    {
        try { feedback.WarmUp(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    private void SafeVibrate()
    {
        try { feedback.Vibrate(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    private void CompleteAttempt()
    {
        if (IsGameOver) return;
        Phase = GameState.GameOver; DurationSeconds = Engine.Elapsed;
        ResultTitle = Score > PreviousBest ? "Новый рекорд!" : "Каждый полёт — опыт";
        MedalText = MedalProgress.Earned(Score); NextMedalText = MedalProgress.Next(Score); MedalFraction = MedalProgress.Fraction(Score);
        if (settings.SoundEnabled) SafePlay("hit"); if (settings.VibrationEnabled) SafeVibrate();
        pendingResult = new GameResult { Id = Engine.SessionId, PlayerName = settings.PlayerName, Score = Score,
            DurationSeconds = DurationSeconds, PlayedAtUtc = DateTime.UtcNow, Difficulty = Engine.Difficulty,
            FlightLocationId = Location.Id };
        SaveResultCommand.Execute(null);
    }
    [RelayCommand]
    private void Pause()
    {
        CancelCountdown(); Engine.Pause(); Phase = Engine.State;
    }
    public void OnBackgrounded() => Pause();
    [RelayCommand]
    private async Task ResumeAsync()
    {
        if (!IsPaused || IsBusy || countdown is not null) return;
        IsChoosingExit = false;
        using var source = new CancellationTokenSource(); countdown = source;
        try
        {
            for (int i = 3; i > 0; i--) { CountdownText = i.ToString(); await Task.Delay(1000, source.Token); }
            Engine.Resume(); Phase = Engine.State;
        }
        catch (OperationCanceledException) { }
        finally { if (ReferenceEquals(countdown, source)) countdown = null; CountdownText = ""; }
    }
    private void CancelCountdown() { countdown?.Cancel(); CountdownText = ""; }
    [RelayCommand]
    private void Finish()
    {
        if (!IsPaused || IsBusy || HasCountdown) return;
        IsChoosingExit = false;
        CancelCountdown(); Engine.Finish(); Score = Engine.Score; CompleteAttempt();
    }
    [RelayCommand]
    private void ChooseExit()
    {
        if (!IsPaused || IsBusy) return;
        CancelCountdown();
        IsChoosingExit = true;
    }

    [RelayCommand]
    private void CancelExit() => IsChoosingExit = false;

    [RelayCommand]
    private async Task ExitWithoutSavingAsync()
    {
        if (!IsPaused || !IsChoosingExit || IsBusy) return;
        if (await TryAsync(() => navigation.GoToAsync(Destination.Home), "Не удалось выйти из игры. Попробуй ещё раз."))
            IsChoosingExit = false;
    }
    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (pendingResult is null || IsBusy) return;
        IsBusy = true; CanRetrySave = false;
        try
        {
            var result = pendingResult;
            var outcome = await Store.SaveResultAsync(result);
            pendingResult = null; BestScore = Math.Max(PreviousBest, result.Score);
            NewAchievementsText = string.Join(" • ", AchievementCatalog.All.Where(x => outcome.NewAchievementIds.Contains(x.Id)).Select(x => x.Title));
            Status = "Результат сохранён на устройстве";
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); CanRetrySave = true; Status = "Не удалось сохранить полёт. Повтори запись или явно откажись от сохранения."; }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private void DiscardResult()
    {
        if (IsBusy) return;
        try { if (pendingResult is not null) Store.DiscardPendingResult(pendingResult.Id); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Status = "Не удалось отменить сохранение. Повтори попытку."; return; }
        pendingResult = null; CanRetrySave = false; Status = "Этот полёт не сохранён";
    }
    [RelayCommand]
    private async Task ReplayAsync()
    {
        if (!IsGameOver || IsBusy) return;
        await PrepareAsync(Engine.Difficulty);
    }
    private async Task LeaveAsync(Destination destination)
    {
        if (IsBusy) return;
        if (pendingResult is not null) { Status = "Сначала сохрани результат или выбери «Не сохранять»."; return; }
        if (IsPlaying || IsPaused) { Pause(); return; }
        await TryAsync(() => navigation.GoToAsync(destination), "Не удалось открыть страницу. Попробуй снова.");
    }
    [RelayCommand] private Task HomeAsync() => LeaveAsync(Destination.Home);
    [RelayCommand] private Task LeaderboardAsync() => LeaveAsync(Destination.Leaderboard);
    [RelayCommand] private Task AchievementsAsync() => LeaveAsync(Destination.Achievements);
    public void HandleBack()
    {
        if (IsPlaying) Pause();
        else if (IsPaused)
        {
            if (IsChoosingExit) CancelExit();
            else ChooseExit();
        }
        else HomeCommand.Execute(null);
    }
}
