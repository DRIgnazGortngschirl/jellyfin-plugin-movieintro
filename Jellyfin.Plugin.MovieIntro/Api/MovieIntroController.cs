using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.MovieIntro.Configuration;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MovieIntro.Api;

/// <summary>
/// Endpoints for the settings page: the clips in the intro folder and rescanning the intro library.
/// </summary>
[ApiController]
[Route("MovieIntro")]
[Authorize(Policy = Policies.RequiresElevation)]
public class MovieIntroController : ControllerBase
{
    private readonly IntroLibrary _introLibrary;
    private readonly IntroLibraryHider _hider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieIntroController"/> class.
    /// </summary>
    /// <param name="introLibrary">Intro library.</param>
    /// <param name="hider">Intro library hider.</param>
    public MovieIntroController(IntroLibrary introLibrary, IntroLibraryHider hider)
    {
        _introLibrary = introLibrary;
        _hider = hider;
    }

    /// <summary>
    /// Lists the video files in the intro folder, read from disk. If some are not in the library yet, a scan of the intro library is queued.
    /// </summary>
    /// <returns>The intro folder state.</returns>
    [HttpGet("Clips")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ClipList>> GetClips()
    {
        var clips = _introLibrary.GetClipFiles()
            .Select(p => new ClipInfo(p, Path.GetRelativePath(PluginConfiguration.IntroFolder, p), _introLibrary.IsInLibrary(p)))
            .ToList();
        var libraryFound = _introLibrary.GetIntroLibraryIds().Count > 0;
        var scanning = libraryFound && clips.Any(c => !c.InLibrary) && _introLibrary.QueueScan(false);

        await _hider.HideForAllUsers().ConfigureAwait(false);

        return new ClipList(PluginConfiguration.IntroFolder, Directory.Exists(PluginConfiguration.IntroFolder), libraryFound, scanning, clips);
    }

    /// <summary>
    /// Queues a scan of the intro library.
    /// </summary>
    /// <returns>204 if queued, 404 if there is no library for the intro folder.</returns>
    [HttpPost("Scan")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Scan()
    {
        return _introLibrary.QueueScan(true) ? NoContent() : NotFound();
    }

    /// <summary>
    /// A video file in the intro folder.
    /// </summary>
    /// <param name="Path">Full path as seen by the server.</param>
    /// <param name="Name">Path relative to the intro folder.</param>
    /// <param name="InLibrary">Whether Jellyfin has scanned the file.</param>
    public record ClipInfo(string Path, string Name, bool InLibrary);

    /// <summary>
    /// The state of the intro folder.
    /// </summary>
    /// <param name="Folder">The intro folder.</param>
    /// <param name="FolderExists">Whether the folder exists in the container.</param>
    /// <param name="LibraryFound">Whether a library holds the folder.</param>
    /// <param name="Scanning">Whether a scan was just queued.</param>
    /// <param name="Clips">The video files.</param>
    public record ClipList(string Folder, bool FolderExists, bool LibraryFound, bool Scanning, IReadOnlyList<ClipInfo> Clips);
}
