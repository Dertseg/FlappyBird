namespace FlappyBird;
public partial class App : Application
{
    private readonly IServiceProvider services;
    private readonly GameViewModel game;
    public App(IServiceProvider services, GameViewModel game) { InitializeComponent(); this.services = services; this.game = game; }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolve pages after application resources exist: their XAML uses shared styles.
        var window = new Window(services.GetRequiredService<AppShell>());
        window.Stopped += (_, _) => game.OnBackgrounded();
        window.Deactivated += (_, _) => game.OnBackgrounded();
        return window;
    }
}
