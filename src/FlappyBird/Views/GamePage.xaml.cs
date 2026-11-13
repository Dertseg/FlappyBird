using System.ComponentModel;
using System.Diagnostics;
using FlappyBird.ViewModels;
using FlappyBird.Views.Drawing;
namespace FlappyBird.Views;
public partial class GamePage : ContentPage
{
    private readonly GameViewModel vm;
    private readonly IDispatcherTimer timer;
    private readonly Stopwatch clock = new();
    private double previous;
    public GamePage(GameViewModel vm)
    {
        InitializeComponent(); this.vm = vm; BindingContext = vm;
        timer = Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(16);
        timer.Tick += (_, _) => { double now = clock.Elapsed.TotalSeconds; vm.Advance(now - previous); previous = now; };
    }
    protected override void OnAppearing()
    {
        base.OnAppearing(); vm.FrameChanged += GameCanvas.Invalidate; vm.PropertyChanged += OnVmChanged;
        GameCanvas.Drawable = new GameRenderer(vm.Engine, vm.Skin, vm.Location);
        UpdateTimer(); GameCanvas.Invalidate();
    }
    protected override void OnDisappearing()
    {
        vm.OnBackgrounded(); timer.Stop(); clock.Stop();
        vm.FrameChanged -= GameCanvas.Invalidate; vm.PropertyChanged -= OnVmChanged; base.OnDisappearing();
    }
    private void OnVmChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(vm.IsPlaying)) UpdateTimer(); }
    private void UpdateTimer()
    {
        if (vm.IsPlaying) { clock.Restart(); previous = 0; timer.Start(); }
        else { timer.Stop(); clock.Stop(); }
    }
    private void OnTouch(object? sender, TouchEventArgs e) => vm.FlapCommand.Execute(null);
    public void HandleBack() => vm.HandleBack();
    protected override bool OnBackButtonPressed() { HandleBack(); return true; }
}
