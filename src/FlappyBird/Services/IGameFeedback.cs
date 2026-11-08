namespace FlappyBird.Services;

public interface IGameFeedback
{
    void Play(string sound);
    void Vibrate();
}
