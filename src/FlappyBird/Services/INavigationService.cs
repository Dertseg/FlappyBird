namespace FlappyBird.Services;
public enum Destination { Home, Game, Leaderboard, Achievements, Settings }
public interface INavigationService { Task GoToAsync(Destination destination); }
