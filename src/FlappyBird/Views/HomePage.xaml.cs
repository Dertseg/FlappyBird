using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class HomePage : ContentPage
{
    private readonly HomeViewModel vm;
    public HomePage(HomeViewModel vm) { InitializeComponent(); this.vm = vm; BindingContext = vm; }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (vm.NeedsReload) await vm.LoadCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        vm.CloseSkinPickerCommand.Execute(null);
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        if (!vm.IsSkinPickerOpen) return base.OnBackButtonPressed();
        vm.CloseSkinPickerCommand.Execute(null);
        return true;
    }
}
