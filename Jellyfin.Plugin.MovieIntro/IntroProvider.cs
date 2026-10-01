using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.MovieIntro.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieIntro;

/// <summary>
/// Returns the configured intro clip for eligible movies and series episodes.
/// </summary>
public class IntroProvider : IIntroProvider
{
    private static readonly char[] _idSeparators = [',', ';', ' ', '\t'];

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
        if (config is null || !config.Enabled || string.IsNullOrWhiteSpace(config.IntroPath))
        {
            return Task.FromResult(Enumerable.Empty<IntroInfo>());
        }

        var eligible = item switch
        {
            Movie => IsMovieEligible(item, config),
            Episode episode => IsEpisodeEligible(episode, config),
            _ => false
        };
        if (!eligible)
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

    private static bool IsMovieEligible(BaseItem movie, PluginConfiguration config)
    {
        return config.ApplyToMovies && (config.ApplyToAllMovies || ParseIds(config.AllowedItemIds).Contains(movie.Id));
    }

    private bool IsEpisodeEligible(Episode episode, PluginConfiguration config)
    {
        if (!config.ApplyToEpisodes)
        {
            return false;
        }

        var allowed = ParseIds(config.AllowedSeriesIds);

        // An explicitly listed episode always gets the intro, whatever the episode mode.
        if (allowed.Contains(episode.Id))
        {
            return true;
        }

        if (!config.ApplyToAllSeries
            && !(episode.SeriesId != Guid.Empty && allowed.Contains(episode.SeriesId))
            && !(episode.SeasonId != Guid.Empty && allowed.Contains(episode.SeasonId)))
        {
            return false;
        }

        return config.EpisodeMode switch
        {
            EpisodeIntroMode.EveryEpisode => true,
            EpisodeIntroMode.FirstEpisodeOfEachSeason => IsFirstOfSeason(episode),
            EpisodeIntroMode.FirstEpisodeOfSeries => IsFirstOfSeries(episode),
            _ => false
        };
    }

    private bool IsFirstOfSeason(Episode episode)
    {
        // Specials never count as "first".
        if (episode.ParentIndexNumber == 0)
        {
            return false;
        }

        IEnumerable<BaseItem> siblings;
        if (episode.SeasonId != Guid.Empty)
        {
            siblings = GetEpisodes(episode.SeasonId);
        }
        else if (episode.SeriesId != Guid.Empty)
        {
            // Episodes without a season folder: treat episodes with the same season number as the season.
            siblings = GetEpisodes(episode.SeriesId).Where(e => e.ParentIndexNumber == episode.ParentIndexNumber);
        }
        else
        {
            return false;
        }

        var own = episode.IndexNumber ?? int.MaxValue;
        return !siblings.Any(e => !e.Id.Equals(episode.Id) && (e.IndexNumber ?? int.MaxValue) < own);
    }

    private bool IsFirstOfSeries(Episode episode)
    {
        // Specials never count as "first".
        if (episode.ParentIndexNumber == 0 || episode.SeriesId == Guid.Empty)
        {
            return false;
        }

        var own = SeriesOrder(episode);
        return !GetEpisodes(episode.SeriesId)
            .Where(e => e.ParentIndexNumber != 0 && !e.Id.Equals(episode.Id))
            .Any(e => SeriesOrder(e).CompareTo(own) < 0);
    }

    private static (int Season, int Episode) SeriesOrder(BaseItem episode)
    {
        return (episode.ParentIndexNumber ?? int.MaxValue, episode.IndexNumber ?? int.MaxValue);
    }

    private IReadOnlyList<BaseItem> GetEpisodes(Guid parentId)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = parentId,
            Recursive = true,
            IncludeItemTypes = [BaseItemKind.Episode],
            IsVirtualItem = false
        });
    }

    /// <summary>
    /// Parses an id list: one entry per line, also split on comma, semicolon and whitespace; text after '#' is a comment.
    /// </summary>
    private static HashSet<Guid> ParseIds(string? text)
    {
        var ids = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return ids;
        }

        foreach (var rawLine in text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            var hash = rawLine.IndexOf('#', StringComparison.Ordinal);
            var line = hash >= 0 ? rawLine[..hash] : rawLine;
            foreach (var entry in line.Split(_idSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(entry, out var id))
                {
                    ids.Add(id);
                }
            }
        }

        return ids;
    }
}
