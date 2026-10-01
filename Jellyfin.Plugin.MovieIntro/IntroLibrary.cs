using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.MovieIntro.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieIntro;

/// <summary>
/// Knows the intro folder: the clips on disk, the libraries that hold it, and how to rescan them.
/// </summary>
public class IntroLibrary
{
    private static readonly HashSet<string> _videoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".mkv", ".mov", ".webm", ".avi", ".ts", ".m2ts", ".mts", ".wmv", ".mpg", ".mpeg", ".ogv", ".flv"
    };

    private static readonly TimeSpan _minScanInterval = TimeSpan.FromMinutes(1);

    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<IntroLibrary> _logger;
    private readonly object _scanLock = new();
    private DateTime _lastScan = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntroLibrary"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="providerManager">Provider manager.</param>
    /// <param name="fileSystem">File system.</param>
    /// <param name="logger">Logger.</param>
    public IntroLibrary(ILibraryManager libraryManager, IProviderManager providerManager, IFileSystem fileSystem, ILogger<IntroLibrary> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Returns whether a path is inside the intro folder.
    /// </summary>
    /// <param name="path">Path as seen by the server.</param>
    /// <returns><c>true</c> if the path is inside the intro folder.</returns>
    public static bool IsInIntroFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Resolve "..", so a path like /intros/../movies/x.mkv does not count as inside the folder.
        var full = Path.GetFullPath(path);
        return full.StartsWith(PluginConfiguration.IntroFolder + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Lists the video files in the intro folder, straight from disk.
    /// </summary>
    /// <returns>Full paths, sorted.</returns>
    public IReadOnlyList<string> GetClipFiles()
    {
        if (!Directory.Exists(PluginConfiguration.IntroFolder))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(PluginConfiguration.IntroFolder, "*", SearchOption.AllDirectories)
                .Where(f => _videoExtensions.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith('.'))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Cannot list {Folder}", PluginConfiguration.IntroFolder);
            return [];
        }
    }

    /// <summary>
    /// Returns the ids of the libraries whose folders all lie in the intro folder.
    /// </summary>
    /// <returns>Library ids.</returns>
    public IReadOnlyList<Guid> GetIntroLibraryIds()
    {
        return _libraryManager.GetVirtualFolders()
            .Where(f => f.Locations.Length > 0 && f.Locations.All(l => l == PluginConfiguration.IntroFolder || IsInIntroFolder(l)))
            .Select(f => Guid.TryParse(f.ItemId, out var id) ? id : Guid.Empty)
            .Where(id => !id.Equals(Guid.Empty))
            .ToList();
    }

    /// <summary>
    /// Returns whether the file at the given path is a library item.
    /// </summary>
    /// <param name="path">Path as seen by the server.</param>
    /// <returns><c>true</c> if Jellyfin knows the file.</returns>
    public bool IsInLibrary(string path)
    {
        return _libraryManager.FindByPath(path, false) is not null;
    }

    /// <summary>
    /// Queues a scan of the intro libraries (like "Scan library" in the dashboard), so new, renamed and removed clips are picked up.
    /// </summary>
    /// <param name="force">Scan even if a scan was queued less than a minute ago.</param>
    /// <returns><c>true</c> if an intro library was found.</returns>
    public bool QueueScan(bool force)
    {
        var libraries = GetIntroLibraryIds();
        if (libraries.Count == 0)
        {
            return false;
        }

        lock (_scanLock)
        {
            if (!force && DateTime.UtcNow - _lastScan < _minScanInterval)
            {
                return true;
            }

            _lastScan = DateTime.UtcNow;
        }

        foreach (var id in libraries)
        {
            _logger.LogInformation("Scanning intro library {Id}", id);
            _providerManager.QueueRefresh(
                id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    MetadataRefreshMode = MetadataRefreshMode.Default,
                    ImageRefreshMode = MetadataRefreshMode.Default
                },
                RefreshPriority.High);
        }

        return true;
    }
}
