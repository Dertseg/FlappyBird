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
    [ObservableProperty] private IReadOnlyList<FlightHistoryRow> flights = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasFlights))] private IReadOnlyList<int> flightScores = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowLeaders)), NotifyPropertyChangedFor(nameof(HasLastRunNote))]
    private bool showHistory;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasLastRunNote))] private string lastRunNote = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DifficultyName)), NotifyPropertyChangedFor(nameof(IsEasy)),
        NotifyPropertyChangedFor(nameof(IsNormal)), NotifyPropertyChangedFor(nameof(IsHard))]
    private Difficulty selectedDifficulty = Difficulty.Normal;
    public bool IsEmpty => Leaders.Count == 0;
    public bool ShowLeaders => !ShowHistory;
    public bool HasFlights => FlightScores.Count > 0;
    public bool HasLastRunNote => ShowLeaders && LastRunNote.Length > 0;
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

        await RefreshAsync(difficulty, latest, ShowHistory);
        latestResult = latest;
        loaded = true;
    }, "Не удалось загрузить полёты. Открой экран снова.");
    private async Task RefreshAsync(Difficulty difficulty, GameResult? last, bool showHistory)
    {
        if (showHistory)
        {
            var recent = await Store.GetRecentAsync(difficulty);
            Flights = recent.Select(x => new FlightHistoryRow(x.Score, FormatDate(x.PlayedAtUtc),
                $"{Math.Floor(x.DurationSeconds / 60):0}:{Math.Floor(x.DurationSeconds % 60):00}")).ToArray();
            FlightScores = recent.Reverse().Select(x => x.Score).ToArray();
        }
        else
        {
            await RefreshLeadersAsync(difficulty, last);
        }

        SelectedDifficulty = difficulty;
        ShowHistory = showHistory;
    }

    private static string FormatDate(DateTime playedAtUtc) => playedAtUtc.ToLocalTime().ToString("dd.MM.yyyy · HH:mm");

    private async Task RefreshLeadersAsync(Difficulty difficulty, GameResult? last)
    {
        var top = await Store.GetTopAsync(difficulty);
        var rows = top.Select((x, i) => new ScoreRow(i + 1, x.PlayerName, x.Score,
            FormatDate(x.PlayedAtUtc), x.Id == last?.Id)).ToArray();
        var note = last is not null && last.Difficulty == difficulty
            ? rows.Any(x => x.IsLatest) ? "Твой последний полёт выделен в таблице" : $"Последний полёт: {last.Score} очк. До попадания в топ‑10 — ещё немного практики!"
            : "";
        Leaders = rows;
        LastRunNote = note;
    }
    [RelayCommand]
    private async Task SelectDifficultyAsync(string value)
    {
        if (!Enum.TryParse<Difficulty>(value, out var difficulty) || !Enum.IsDefined(difficulty)) return;
        await TryAsync(() => RefreshAsync(difficulty, latestResult, ShowHistory), "Не удалось загрузить этот уровень. Повтори выбор.");
    }

    [RelayCommand]
    private Task SelectListAsync(string value)
    {
        bool history = value == "History";
        if (history == ShowHistory) return Task.CompletedTask;
        return TryAsync(() => RefreshAsync(SelectedDifficulty, latestResult, history),
            "Не удалось переключить список. Попробуй ещё раз.");
    }
}
