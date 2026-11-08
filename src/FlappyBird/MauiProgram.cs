namespace FlappyBird;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton<IGameStore>(_ => new SqliteGameStore(Path.Combine(FileSystem.AppDataDirectory, "flappybird.db")));
        builder.Services.AddSingleton<IGameFeedback, AndroidFeedback>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<AppShell>();
        return builder.Build();
    }
}
