using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MovieIntro.Configuration;

/// <summary>
/// Which episodes of an eligible series get the intro.
/// </summary>
public enum EpisodeIntroMode
{
    /// <summary>
    /// Every episode gets the intro.
    /// </summary>
    EveryEpisode = 0,

    /// <summary>
    /// Only the first episode of each season gets the intro.
    /// </summary>
    FirstEpisodeOfEachSeason = 1,

    /// <summary>
    /// Only the first episode of the series (lowest season, lowest episode; specials excluded) gets the intro.
    /// </summary>
    FirstEpisodeOfSeries = 2
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether intros are served at all.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the full path of the intro clip as seen by the server. It must be inside a library.
    /// </summary>
    public string IntroPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether movies can get the intro at all.
    /// </summary>
    public bool ApplyToMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether every movie gets the intro (ignores the allow-list).
    /// </summary>
    public bool ApplyToAllMovies { get; set; }

    /// <summary>
    /// Gets or sets the movie item ids (one per line; comma, semicolon or space separated; '#' starts a comment) that get the intro.
    /// </summary>
    public string AllowedItemIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether series episodes can get the intro at all.
    /// </summary>
    public bool ApplyToEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether episodes of every series are eligible (ignores the series allow-list).
    /// </summary>
    public bool ApplyToAllSeries { get; set; }

    /// <summary>
    /// Gets or sets the series, season or episode ids (same format as <see cref="AllowedItemIds"/>) that get the intro.
    /// Listed episodes always get it; episodes of listed series or seasons get it according to <see cref="EpisodeMode"/>.
    /// </summary>
    public string AllowedSeriesIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets which episodes of an eligible series get the intro.
    /// </summary>
    public EpisodeIntroMode EpisodeMode { get; set; } = EpisodeIntroMode.EveryEpisode;
}
