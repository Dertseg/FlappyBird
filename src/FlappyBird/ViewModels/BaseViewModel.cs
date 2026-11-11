using CommunityToolkit.Mvvm.ComponentModel;
using FlappyBird.Data;
namespace FlappyBird.ViewModels;

public abstract partial class BaseViewModel(IGameStore store) : ObservableObject
{
    protected IGameStore Store { get; } = store;
    private long loadedRevision = -1;
    public bool NeedsReload => loadedRevision != Store.Revision;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract))] private bool isBusy;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStatus))] private string status = "";
    public bool CanInteract => !IsBusy;
    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);

    protected async Task LoadPageAsync(Func<Task> load, string failure)
    {
        long revision = Store.Revision;
        if (await TryAsync(load, failure)) loadedRevision = revision;
    }

    protected async Task<bool> TryAsync(Func<Task> action, string failure)
    {
        if (IsBusy) return false;
        IsBusy = true; Status = "";
        try { await action(); return true; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Status = failure; return false; }
        finally { IsBusy = false; }
    }
}
