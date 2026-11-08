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
        var settings = await Store.GetSettingsAsync();
        Greeting = $"{settings.PlayerName}, пора летать!"; SelectedDifficulty = settings.DefaultDifficulty;
        await RefreshScoreAsync();
        Attempts = (await Store.GetStatisticsAsync()).Attempts;
        var achievements = await Store.GetAchievementsAsync();
        AchievementsSummary = $"{achievements.Count(x => x.IsUnlocked)} из {achievements.Count} достижений";
    }, "Не удалось загрузить данные. Проверь свободное место и нажми «Обновить».");
    private async Task RefreshScoreAsync() => BestScore = (await Store.GetTopAsync(SelectedDifficulty)).FirstOrDefault()?.Score ?? 0;

    [RelayCommand]
    private async Task SelectDifficultyAsync(string value)
    {
        if (!Enum.TryParse<Difficulty>(value, out var difficulty) || !Enum.IsDefined(difficulty)) return;
        await TryAsync(async () =>
        {
            var settings = await Store.GetSettingsAsync(); settings.DefaultDifficulty = difficulty;
            await Store.SaveSettingsAsync(settings); SelectedDifficulty = difficulty; await RefreshScoreAsync();
        }, "Не удалось сохранить уровень. Повтори выбор.");
    }
    [RelayCommand]
    private async Task StartAsync() => await TryAsync(async () =>
    {
        if (await game.PrepareAsync(SelectedDifficulty)) await navigation.GoToAsync(Destination.Game);
        else Status = game.Status;
    }, "Не удалось начать игру. Попробуй ещё раз.");
    [RelayCommand] private Task AchievementsAsync() => navigation.GoToAsync(Destination.Achievements);
    [RelayCommand] private Task LeaderboardAsync() => navigation.GoToAsync(Destination.Leaderboard);
}
