using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class AchievementsPage : ContentPage
{
    private readonly AchievementsViewModel vm;
    public AchievementsPage(AchievementsViewModel vm) { InitializeComponent(); this.vm = vm; BindingContext = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await vm.LoadCommand.ExecuteAsync(null); }
}
