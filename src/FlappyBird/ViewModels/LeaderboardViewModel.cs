using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlappyBird.Data;
using FlappyBird.Models;
namespace FlappyBird.ViewModels;

public partial class LeaderboardViewModel(IGameStore store) : BaseViewModel(store)
{
    private GameResult? latestResult;
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
        var latest = await Store.GetLatestResultAsync();
        var difficulty = SelectedDifficulty;
        if (latest is not null && latest.Id != latestResult?.Id)
            difficulty = latest.Difficulty;
        else if (!loaded)
            difficulty = (await Store.GetSettingsAsync()).DefaultDifficulty;

        await RefreshAsync(difficulty, latest);
        latestResult = latest;
        loaded = true;
    }, "Не удалось загрузить рекорды. Нажми «Обновить».");
    private async Task RefreshAsync(Difficulty difficulty, GameResult? last)
    {
        var top = await Store.GetTopAsync(difficulty);
        var rows = top.Select((x, i) => new ScoreRow(i + 1, x.PlayerName, x.Score,
            x.PlayedAtUtc.ToLocalTime().ToString("dd.MM.yyyy · HH:mm"), x.Id == last?.Id)).ToArray();
        var note = last is not null && last.Difficulty == difficulty
            ? rows.Any(x => x.IsLatest) ? "Твой последний полёт выделен в таблице" : $"Последний полёт: {last.Score} очк. До попадания в топ‑10 — ещё немного практики!"
            : "";
        SelectedDifficulty = difficulty;
        Leaders = rows;
        LastRunNote = note;
    }
    [RelayCommand]
    private async Task SelectDifficultyAsync(string value)
    {
        if (!Enum.TryParse<Difficulty>(value, out var difficulty) || !Enum.IsDefined(difficulty)) return;
        await TryAsync(() => RefreshAsync(difficulty, latestResult), "Не удалось загрузить этот уровень. Повтори выбор.");
    }
}
