using System.Diagnostics;
using FlappyBird.Core;
using FlappyBird.Drawing;
namespace FlappyBird;
public partial class MainPage : ContentPage
{
    private readonly MainViewModel vm;
    private readonly IDispatcherTimer timer;
    private readonly Stopwatch clock = new();
    private double previous;
    public MainPage(MainViewModel vm)
    {
        InitializeComponent(); this.vm = vm; BindingContext = vm;
        GameCanvas.Drawable = new GameRenderer(vm.Engine);
        vm.FrameChanged += GameCanvas.Invalidate;
        timer = Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(16);
        timer.Tick += (_, _) => { double now = clock.Elapsed.TotalSeconds; vm.Advance(now - previous); previous = now; };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); clock.Restart(); previous = 0; timer.Start(); await vm.InitializeAsync();
    }
    private void OnTouch(object? sender, TouchEventArgs e) => vm.FlapCommand.Execute(null);
    public void Backgrounded() { vm.OnBackgrounded(); timer.Stop(); }
    public void Foregrounded() { previous = clock.Elapsed.TotalSeconds; timer.Start(); }
    protected override bool OnBackButtonPressed()
    {
        if (vm.IsGame) { if (vm.IsGameOver || vm.IsPaused) vm.MenuCommand.Execute(null); else vm.PauseCommand.Execute(null); return true; }
        if (!vm.IsMenu) { vm.MenuCommand.Execute(null); return true; }
        return base.OnBackButtonPressed();
    }
}
