namespace FlappyBird.Services;

public interface IGameFeedback
{
    void WarmUp() { }
    void Play(string sound);
    void Vibrate();
}
