using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieIntro;

/// <summary>
/// Hides the libraries that hold the intro folder from every user's home screen, side menu and "latest" rows.
/// Users keep access to them, because the clip must stay playable for cinema mode.
/// </summary>
public class IntroLibraryHider : IHostedService, IEventConsumer<UserCreatedEventArgs>
{
    private readonly IntroLibrary _introLibrary;
    private readonly IUserManager _userManager;
    private readonly ILogger<IntroLibraryHider> _logger;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroLibraryHider"/> class.
    /// </summary>
    /// <param name="introLibrary">Intro library.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="logger">Logger.</param>
    public IntroLibraryHider(IntroLibrary introLibrary, IUserManager userManager, ILogger<IntroLibraryHider> logger)
    {
        _introLibrary = introLibrary;
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
        return HideFor(eventArgs.Argument, _introLibrary.GetIntroLibraryIds());
    }

    /// <summary>
    /// Hides the intro libraries for all users.
    /// </summary>
    /// <returns><c>true</c> if an intro library was found.</returns>
    public async Task<bool> HideForAllUsers()
    {
        var libraries = _introLibrary.GetIntroLibraryIds();
        if (libraries.Count == 0)
        {
            return false;
        }

        foreach (var user in _userManager.GetUsers().ToList())
        {
            await HideFor(user, libraries).ConfigureAwait(false);
        }

        return true;
    }

    private async Task HideFor(User user, IReadOnlyList<Guid> libraries)
    {
        var changed = false;
        foreach (var kind in new[] { PreferenceKind.MyMediaExcludes, PreferenceKind.LatestItemExcludes })
        {
            var current = user.GetPreferenceValues<Guid>(kind);
            var missing = libraries.Where(id => !current.Contains(id)).ToArray();
            if (missing.Length > 0)
            {
                user.SetPreference(kind, current.Concat(missing).ToArray());
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
