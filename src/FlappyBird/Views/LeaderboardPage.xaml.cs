using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class LeaderboardPage : ContentPage
{
    private readonly LeaderboardViewModel vm;
    public LeaderboardPage(LeaderboardViewModel vm) { InitializeComponent(); this.vm = vm; BindingContext = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await vm.LoadCommand.ExecuteAsync(null); }
}
