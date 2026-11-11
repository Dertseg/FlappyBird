namespace FlappyBird;
public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        HomeContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<HomePage>());
        RecordsContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<LeaderboardPage>());
        AchievementsContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<AchievementsPage>());
        SettingsContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<SettingsPage>());
        Routing.RegisterRoute("game", typeof(GamePage));
    }
    protected override bool OnBackButtonPressed()
    {
        if (CurrentPage is GamePage page) { page.HandleBack(); return true; }
        return base.OnBackButtonPressed();
    }
}
