namespace FlappyBird;
public partial class App : Application
{
    private readonly MainPage page;
    public App(MainPage page) { InitializeComponent(); this.page = page; }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(page);
        window.Stopped += (_, _) => page.Backgrounded();
        window.Deactivated += (_, _) => page.Backgrounded();
        window.Activated += (_, _) => page.Foregrounded();
        return window;
    }
}
