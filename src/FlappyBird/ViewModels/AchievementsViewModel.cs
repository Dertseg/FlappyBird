using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlappyBird.Data;
using FlappyBird.Models;
namespace FlappyBird.ViewModels;

public partial class AchievementsViewModel(IGameStore store) : BaseViewModel(store)
{
    [ObservableProperty] private IReadOnlyList<AchievementProgress> achievements = [];
    [ObservableProperty] private string summary = "Каждый полёт приближает к цели";
    [ObservableProperty] private double completion;
    [RelayCommand]
    private Task LoadAsync() => LoadPageAsync(async () =>
    {
        Achievements = await Store.GetAchievementsAsync();
        int earned = Achievements.Count(x => x.IsUnlocked);
        Summary = $"Открыто {earned} из {Achievements.Count}";
        Completion = Achievements.Count == 0 ? 0 : (double)earned / Achievements.Count;
    }, "Не удалось загрузить достижения. Открой этот экран снова.");
}
