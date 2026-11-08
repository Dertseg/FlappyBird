namespace FlappyBird;

public partial class AppShell : Shell
{
    private readonly MainPage page;
    public AppShell(MainPage page)
    {
        InitializeComponent();
        this.page = page;
        Items.Add(new ShellContent { Route = "game", Content = page });
    }
    protected override bool OnBackButtonPressed() => page.HandleBackButton();
}
