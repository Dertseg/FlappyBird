using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlappyBird.Data;
using FlappyBird.Models;
using FlappyBird.Services;
namespace FlappyBird.ViewModels;

public partial class LeaderboardViewModel(IGameStore store, GameSession session) : BaseViewModel(store)
{
    private Guid? seenResult;
    private bool loaded;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsEmpty))] private IReadOnlyList<ScoreRow> leaders = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasLastRunNote))] private string lastRunNote = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DifficultyName)), NotifyPropertyChangedFor(nameof(IsEasy)),
        NotifyPropertyChangedFor(nameof(IsNormal)), NotifyPropertyChangedFor(nameof(IsHard))]
    private Difficulty selectedDifficulty = Difficulty.Normal;
    public bool IsEmpty => Leaders.Count == 0;
    public bool HasLastRunNote => LastRunNote.Length > 0;
    public string DifficultyName => DifficultyCatalog.Get(SelectedDifficulty).Name;
    public bool IsEasy => SelectedDifficulty == Difficulty.Easy;
    public bool IsNormal => SelectedDifficulty == Difficulty.Normal;
    public bool IsHard => SelectedDifficulty == Difficulty.Hard;
    [RelayCommand]
    private async Task LoadAsync() => await TryAsync(async () =>
    {
        if (session.LastSavedResult is { } latest && latest.Id != seenResult)
        { SelectedDifficulty = latest.Difficulty; seenResult = latest.Id; }
        else if (!loaded) SelectedDifficulty = (await Store.GetSettingsAsync()).DefaultDifficulty;
        await RefreshAsync(); loaded = true;
    }, "Не удалось загрузить рекорды. Нажми «Обновить».");
    private async Task RefreshAsync()
    {
        var top = await Store.GetTopAsync(SelectedDifficulty);
        var last = session.LastSavedResult;
        Leaders = top.Select((x, i) => new ScoreRow(i + 1, x.PlayerName, x.Score,
            x.PlayedAtUtc.ToLocalTime().ToString("dd.MM.yyyy · HH:mm"), x.Id == last?.Id)).ToArray();
        LastRunNote = last is not null && last.Difficulty == SelectedDifficulty
            ? Leaders.Any(x => x.IsLatest) ? "Твой последний полёт выделен в таблице" : $"Последний полёт: {last.Score} очк. До попадания в топ‑10 — ещё немного практики!"
            : "";
    }
    [RelayCommand]
    private async Task SelectDifficultyAsync(string value)
    {
        if (!Enum.TryParse<Difficulty>(value, out var difficulty) || !Enum.IsDefined(difficulty)) return;
        await TryAsync(async () => { SelectedDifficulty = difficulty; await RefreshAsync(); }, "Не удалось загрузить этот уровень. Повтори выбор.");
    }
}
