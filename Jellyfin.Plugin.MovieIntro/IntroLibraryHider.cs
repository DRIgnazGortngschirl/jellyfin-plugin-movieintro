using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieIntro;

/// <summary>
/// Hides the library that holds the intro clip from every user's home screen, side menu and "latest" rows.
/// Users keep access to it, because the clip must stay playable for cinema mode.
/// </summary>
public class IntroLibraryHider : IHostedService, IEventConsumer<UserCreatedEventArgs>
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<IntroLibraryHider> _logger;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroLibraryHider"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="logger">Logger.</param>
    public IntroLibraryHider(ILibraryManager libraryManager, IUserManager userManager, ILogger<IntroLibraryHider> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        // The library is not ready while the host starts; retry for a while so a fresh scan is also covered.
        _ = Task.Run(
            async () =>
            {
                for (var i = 0; i < 10 && !token.IsCancellationRequested; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
                    if (await HideForAllUsers().ConfigureAwait(false))
                    {
                        return;
                    }
                }
            },
            token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnEvent(UserCreatedEventArgs eventArgs)
    {
        var library = GetIntroLibraryId();
        return library.HasValue ? HideFor(eventArgs.Argument, library.Value) : Task.CompletedTask;
    }

    /// <summary>
    /// Hides the intro library for all users.
    /// </summary>
    /// <returns><c>true</c> if the intro library was found.</returns>
    public async Task<bool> HideForAllUsers()
    {
        var library = GetIntroLibraryId();
        if (!library.HasValue)
        {
            return false;
        }

        foreach (var user in _userManager.GetUsers().ToList())
        {
            await HideFor(user, library.Value).ConfigureAwait(false);
        }

        return true;
    }

    private Guid? GetIntroLibraryId()
    {
        var path = Plugin.Instance?.Configuration.IntroPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var intro = _libraryManager.FindByPath(path, false);
        if (intro is null)
        {
            return null;
        }

        var folder = _libraryManager.GetCollectionFolders(intro).FirstOrDefault();
        if (folder is null)
        {
            return null;
        }

        var items = _libraryManager.GetItemList(new InternalItemsQuery { ParentId = folder.Id, Recursive = true, IsFolder = false, Limit = 2 });
        if (items.Count > 1)
        {
            // Never hide a library that holds anything besides the intro clip.
            _logger.LogWarning("Intro clip shares library {Name} with other items; not hiding it", folder.Name);
            return null;
        }

        return folder.Id;
    }

    private async Task HideFor(User user, Guid libraryId)
    {
        var changed = false;
        foreach (var kind in new[] { PreferenceKind.MyMediaExcludes, PreferenceKind.LatestItemExcludes })
        {
            var current = user.GetPreferenceValues<Guid>(kind);
            if (!current.Contains(libraryId))
            {
                user.SetPreference(kind, current.Append(libraryId).ToArray());
                changed = true;
            }
        }

        if (changed)
        {
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
            _logger.LogInformation("Hid intro library from home screen of user {User}", user.Username);
        }
    }
}
