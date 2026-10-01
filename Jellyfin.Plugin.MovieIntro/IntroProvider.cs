using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.MovieIntro.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieIntro;

/// <summary>
/// Returns the configured intro clip for movies and/or series episodes.
/// </summary>
public class IntroProvider : IIntroProvider
{
    private readonly ILibraryManager _libraryManager;
    private readonly IntroLibrary _introLibrary;
    private readonly ILogger<IntroProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="introLibrary">Intro library.</param>
    /// <param name="logger">Logger.</param>
    public IntroProvider(ILibraryManager libraryManager, IntroLibrary introLibrary, ILogger<IntroProvider> logger)
    {
        _libraryManager = libraryManager;
        _introLibrary = introLibrary;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Movie Intro";

    /// <inheritdoc />
    public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || string.IsNullOrWhiteSpace(config.IntroPath))
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        var eligible = item switch
        {
            Movie => config.ApplyToMovies,
            Episode => config.ApplyToEpisodes,
            _ => false
        };
        if (!eligible)
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        if (!IntroLibrary.IsInIntroFolder(config.IntroPath))
        {
            _logger.LogWarning("Intro clip {Path} is outside {Folder}; not serving it", config.IntroPath, PluginConfiguration.IntroFolder);
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        var intro = _libraryManager.FindByPath(config.IntroPath, false);
        if (intro is null)
        {
            if (!File.Exists(config.IntroPath))
            {
                _logger.LogWarning("Intro clip {Path} does not exist (renamed or deleted?); choose it again on the settings page", config.IntroPath);
            }
            else if (_introLibrary.QueueScan(false))
            {
                _logger.LogWarning("Intro clip {Path} is not scanned yet; scanning the intro library", config.IntroPath);
            }
            else
            {
                _logger.LogWarning("Intro clip {Path} is not in any library; add {Folder} as a library", config.IntroPath, PluginConfiguration.IntroFolder);
            }

            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        if (intro.Id.Equals(item.Id))
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        _logger.LogInformation("Serving intro {IntroId} before {Name}", intro.Id, item.Name);
        return Task.FromResult<IEnumerable<IntroInfo>>([new IntroInfo { ItemId = intro.Id }]);
    }
}
