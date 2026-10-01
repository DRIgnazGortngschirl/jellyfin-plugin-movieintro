using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MovieIntro.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// The folder (as seen by the server) the intro clip must be in.
    /// </summary>
    public const string IntroFolder = "/intros";

    /// <summary>
    /// Gets or sets the full path of the intro clip as seen by the server. It must be inside <see cref="IntroFolder"/> and a library.
    /// </summary>
    public string IntroPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the intro plays before movies.
    /// </summary>
    public bool ApplyToMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the intro plays before series episodes.
    /// </summary>
    public bool ApplyToEpisodes { get; set; }
}
