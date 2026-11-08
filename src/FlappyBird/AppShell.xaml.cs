namespace FlappyBird;
public partial class AppShell : Shell
{
    public AppShell(HomePage home, LeaderboardPage records, AchievementsPage achievements, SettingsPage settings)
    {
        InitializeComponent(); HomeContent.Content = home; RecordsContent.Content = records;
        AchievementsContent.Content = achievements; SettingsContent.Content = settings;
        Routing.RegisterRoute("game", typeof(GamePage));
    }
    protected override bool OnBackButtonPressed()
    {
        if (CurrentPage is GamePage page) { page.HandleBack(); return true; }
        return base.OnBackButtonPressed();
    }
}
