namespace FlappyBird.Services;
public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(Destination destination) => Shell.Current.GoToAsync(destination switch
    {
        Destination.Home => "//home", Destination.Game => "game", Destination.Leaderboard => "//records",
        Destination.Achievements => "//achievements", Destination.Settings => "//settings",
        _ => throw new ArgumentOutOfRangeException(nameof(destination))
    });
}
