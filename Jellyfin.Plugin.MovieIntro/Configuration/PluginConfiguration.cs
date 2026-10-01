using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MovieIntro.Configuration;

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
    public string IntroPath { get; set; } = "/intros/mario-streaming-intro.mp4";

    /// <summary>
    /// Gets or sets a value indicating whether every movie gets the intro (ignores the allow-list).
    /// </summary>
    public bool ApplyToAllMovies { get; set; }

    /// <summary>
    /// Gets or sets the movie item ids (comma or newline separated) that get the intro.
    /// </summary>
    public string AllowedItemIds { get; set; } = string.Empty;
}
