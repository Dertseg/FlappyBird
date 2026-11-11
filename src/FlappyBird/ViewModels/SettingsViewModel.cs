using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlappyBird.Data;
using FlappyBird.Models;
namespace FlappyBird.ViewModels;

public partial class SettingsViewModel(IGameStore store) : BaseViewModel(store)
{
    [ObservableProperty] private string playerName = "Игрок";
    [ObservableProperty] private bool soundEnabled = true;
    [ObservableProperty] private bool vibrationEnabled = true;
    [ObservableProperty] private DifficultyOption selectedDifficulty = DifficultyCatalog.Get(Difficulty.Normal);
    public IReadOnlyList<DifficultyOption> Difficulties => DifficultyCatalog.All;
    [RelayCommand]
    private Task LoadAsync() => LoadPageAsync(async () =>
    {
        var settings = await Store.GetSettingsAsync();
        PlayerName = settings.PlayerName; SoundEnabled = settings.SoundEnabled; VibrationEnabled = settings.VibrationEnabled;
        SelectedDifficulty = DifficultyCatalog.Get(settings.DefaultDifficulty);
    }, "Не удалось загрузить настройки. Нажми «Обновить».");
    [RelayCommand]
    private async Task SaveAsync() => await TryAsync(async () =>
    {
        var name = PlayerName.Trim();
        if (name.Length == 0) name = "Игрок";
        if (name.Length > 20) { Status = "Имя должно быть не длиннее 20 символов."; return; }
        await Store.SaveSettingsAsync(new PlayerSettings { PlayerName = name, SoundEnabled = SoundEnabled,
            VibrationEnabled = VibrationEnabled, DefaultDifficulty = SelectedDifficulty.Id });
        PlayerName = name; Status = "Настройки сохранены";
    }, "Не удалось сохранить настройки. Повтори попытку.");
}
