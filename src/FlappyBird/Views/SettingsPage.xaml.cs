using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel vm;
    public SettingsPage(SettingsViewModel vm) { InitializeComponent(); this.vm = vm; BindingContext = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await vm.LoadCommand.ExecuteAsync(null); }
}
