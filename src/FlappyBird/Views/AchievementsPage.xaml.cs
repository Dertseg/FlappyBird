using FlappyBird.ViewModels;
using FlappyBird.Data;
namespace FlappyBird.Views;
public partial class AchievementsPage : ContentPage
{
    private readonly AchievementsViewModel vm;
    private readonly IGameStore store;
    private bool visible;
    public AchievementsPage(AchievementsViewModel vm, IGameStore store)
    {
        InitializeComponent();
        this.vm = vm;
        this.store = store;
        BindingContext = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        visible = true;
        store.ResultsChanged += Refresh;
        if (vm.NeedsReload) await vm.LoadCommand.ExecuteAsync(null);
    }
    protected override void OnDisappearing()
    {
        visible = false;
        store.ResultsChanged -= Refresh;
        base.OnDisappearing();
    }
    private void Refresh() => Dispatcher.Dispatch(async () =>
    {
        if (vm.LoadCommand.ExecutionTask is { IsCompleted: false } loading) await loading;
        if (visible && vm.NeedsReload) await vm.LoadCommand.ExecuteAsync(null);
    });
}
