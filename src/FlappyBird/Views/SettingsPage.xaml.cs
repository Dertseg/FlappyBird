using FlappyBird.ViewModels;
namespace FlappyBird.Views;
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel vm;
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        this.vm = vm;
        BindingContext = vm;
        VersionLabel.Text = $"FLAPPY / CLUB · {AppInfo.Current.VersionString}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (vm.NeedsReload && !vm.IsDirty) await vm.LoadCommand.ExecuteAsync(null);
    }
}
