using System.ComponentModel;
using Android.Views;
using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class HomePage : ContentPage
{
    private readonly HomeViewModel vm;
    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        this.vm = vm;
        BindingContext = vm;
        vm.PropertyChanged += OnViewModelChanged;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.IsSkinPickerOpen) &&
            HomeContent.Handler?.PlatformView is Android.Views.View background)
        {
            background.ImportantForAccessibility = vm.IsSkinPickerOpen
                ? ImportantForAccessibility.NoHideDescendants
                : ImportantForAccessibility.Auto;
        }
    }
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
