using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieIntro;

/// <summary>
/// Returns the configured intro clip for allow-listed movies.
/// </summary>
public class IntroProvider : IIntroProvider
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<IntroProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="logger">Logger.</param>
    public IntroProvider(ILibraryManager libraryManager, ILogger<IntroProvider> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Movie Intro";

    /// <inheritdoc />
    public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.Enabled || item is not Movie || string.IsNullOrWhiteSpace(config.IntroPath))
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        if (!config.ApplyToAllMovies && !IsAllowed(item.Id, config.AllowedItemIds))
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        var intro = _libraryManager.FindByPath(config.IntroPath, false);
        if (intro is null)
        {
            _logger.LogWarning("Intro clip {Path} is not in any library yet; add its folder as a library and scan it", config.IntroPath);
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        if (intro.Id.Equals(item.Id))
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        _logger.LogInformation("Serving intro {IntroId} before {Name}", intro.Id, item.Name);
        return Task.FromResult<IEnumerable<IntroInfo>>([new IntroInfo { ItemId = intro.Id }]);
    }

    private static bool IsAllowed(Guid id, string allowed)
    {
        return allowed
            .Split([',', '\n', '\r', ' ', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Any(s => Guid.TryParse(s, out var g) && g.Equals(id));
    }
}
