namespace FlappyBird;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton<IGameStore>(_ => new SqliteGameStore(Path.Combine(FileSystem.AppDataDirectory, "flappybird.db")));
        builder.Services.AddSingleton<IGameFeedback, AndroidFeedback>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<GameSession>();
        builder.Services.AddSingleton<GameViewModel>();
        builder.Services.AddSingleton<HomeViewModel>(); builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<LeaderboardViewModel>(); builder.Services.AddSingleton<LeaderboardPage>();
        builder.Services.AddSingleton<AchievementsViewModel>(); builder.Services.AddSingleton<AchievementsPage>();
        builder.Services.AddSingleton<SettingsViewModel>(); builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddTransient<GamePage>();
        builder.Services.AddSingleton<AppShell>();
        return builder.Build();
    }
}
