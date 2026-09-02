// Contract for navigating between application screens.
namespace MediaPlayerApp.Application.Interfaces;

public interface INavigationService
{
    /// <summary>
    /// Navigates to the Show Detail screen for the given show.
    /// </summary>
    void NavigateToShowDetail(string showFolderPath);

    /// <summary>
    /// Navigates to the Player screen for the given episode.
    /// </summary>
    void NavigateToPlayer(string episodeKey);

    /// <summary>
    /// Navigates back to the Home screen.
    /// </summary>
    void NavigateToHome();
}
