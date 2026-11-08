namespace FlappyBird;
public partial class App : Application
{
    private readonly MainPage page;
    private readonly AppShell shell;
    public App(AppShell shell, MainPage page) { InitializeComponent(); this.shell = shell; this.page = page; }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(shell);
        window.Stopped += (_, _) => page.Backgrounded();
        window.Deactivated += (_, _) => page.Backgrounded();
        window.Activated += (_, _) => page.Foregrounded();
        return window;
    }
}
