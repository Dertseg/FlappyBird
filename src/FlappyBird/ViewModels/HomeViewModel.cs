using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlappyBird.Data;
using FlappyBird.Models;
using FlappyBird.Services;
namespace FlappyBird.ViewModels;

public partial class HomeViewModel(IGameStore store, INavigationService navigation, GameViewModel game) : BaseViewModel(store)
{
    [ObservableProperty] private string greeting = "Готов к новому рекорду?";
    [ObservableProperty] private int bestScore;
    [ObservableProperty] private int attempts;
    [ObservableProperty] private string achievementsSummary = "0 из 4 достижений";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DifficultyName)), NotifyPropertyChangedFor(nameof(DifficultyDescription)),
        NotifyPropertyChangedFor(nameof(IsEasy)), NotifyPropertyChangedFor(nameof(IsNormal)), NotifyPropertyChangedFor(nameof(IsHard))]
    private Difficulty selectedDifficulty = Difficulty.Normal;
    public string DifficultyName => DifficultyCatalog.Get(SelectedDifficulty).Name;
    public string DifficultyDescription => DifficultyCatalog.Get(SelectedDifficulty).Description;
    public bool IsEasy => SelectedDifficulty == Difficulty.Easy;
    public bool IsNormal => SelectedDifficulty == Difficulty.Normal;
    public bool IsHard => SelectedDifficulty == Difficulty.Hard;

    [RelayCommand]
    private async Task LoadAsync() => await TryAsync(async () =>
    {
        var summary = await Store.GetHomeSummaryAsync();
        Greeting = $"{summary.Settings.PlayerName}, пора летать!";
        SelectedDifficulty = summary.Settings.DefaultDifficulty;
        BestScore = summary.BestScore;
        Attempts = summary.Attempts;
        AchievementsSummary = $"{summary.UnlockedAchievements} из {AchievementCatalog.All.Count} достижений";
    }, "Не удалось загрузить данные. Проверь свободное место и нажми «Обновить».");

    [RelayCommand]
    private async Task SelectDifficultyAsync(string value)
    {
        if (!Enum.TryParse<Difficulty>(value, out var difficulty) || !Enum.IsDefined(difficulty)) return;
        await TryAsync(async () =>
        {
            var settings = await Store.GetSettingsAsync(); settings.DefaultDifficulty = difficulty;
            int best = await Store.GetBestScoreAsync(difficulty);
            await Store.SaveSettingsAsync(settings);
            SelectedDifficulty = difficulty;
            BestScore = best;
        }, "Не удалось сохранить уровень. Повтори выбор.");
    }
    [RelayCommand]
    private async Task StartAsync() => await TryAsync(async () =>
    {
        if (await game.PrepareAsync(SelectedDifficulty)) await navigation.GoToAsync(Destination.Game);
        else Status = game.Status;
    }, "Не удалось начать игру. Попробуй ещё раз.");
    [RelayCommand]
    private Task AchievementsAsync() => TryAsync(() => navigation.GoToAsync(Destination.Achievements),
        "Не удалось открыть достижения. Попробуй ещё раз.");

    [RelayCommand]
    private Task LeaderboardAsync() => TryAsync(() => navigation.GoToAsync(Destination.Leaderboard),
        "Не удалось открыть рекорды. Попробуй ещё раз.");
}
