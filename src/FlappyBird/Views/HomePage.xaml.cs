using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class HomePage : ContentPage
{
    private readonly HomeViewModel vm;
    public HomePage(HomeViewModel vm) { InitializeComponent(); this.vm = vm; BindingContext = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await vm.LoadCommand.ExecuteAsync(null); }
}
