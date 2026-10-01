# Jellyfin Movie Intro

Plays a short intro clip of your choice before movies and/or series episodes, using Jellyfin's built-in
cinema-mode intro mechanism (`IIntroProvider`). Your media files are never modified.
Targets Jellyfin 12.1 (`net10.0`).

## Features

- **Pick the clip on the settings page** from a dropdown of the videos in your libraries, or type its path.
- **Movies:** play the intro before every movie or only before movies you list.
- **Series:** play the intro before every episode, only the first episode of each season, or only the
  first episode of a series — for every series, or only for the series, seasons and episodes you list.
- **Search helpers** on the settings page to find movies, series, seasons and episodes and add their IDs
  to the lists (with the title as a comment, so the lists stay readable).
- **Hides the intro library** from every user's home screen, side menu and "Latest" rows, while keeping
  the clip playable.
- Nothing is re-encoded or written to your media; turning the plugin off restores normal playback at once.

## Install

### From the plugin repository (recommended)

1. Dashboard → **Plugins** → **Repositories** (or **Catalog** → ⚙) → **+** and add
   `https://raw.githubusercontent.com/DRIgnazGortngschirl/jellyfin-plugin-movieintro/main/manifest.json`
   (any name, e.g. *Movie Intro*).
2. Dashboard → **Plugins** → **Catalog**, install **Movie Intro**.
3. Restart Jellyfin.
4. Continue with [Prepare the intro clip](#prepare-the-intro-clip) and [Configure](#configure).

New releases show up as updates in the catalog.

### Manually

1. Download `movie-intro_<version>.zip` from the
   [releases page](https://github.com/DRIgnazGortngschirl/jellyfin-plugin-movieintro/releases), or
   [build it yourself](#build).
2. Create the folder `<jellyfin config>/data/plugins/Movie Intro_<version>/` (e.g.
   `/config/data/plugins/Movie Intro_0.2.0.0/` in the official Docker image) and put
   `Jellyfin.Plugin.MovieIntro.dll` and `meta.json` in it.
3. Restart Jellyfin. The plugin appears under Dashboard → **Plugins** → **My Plugins**.
4. Continue with the steps below.

## Prepare the intro clip

Jellyfin can only play an intro that is an item in one of its libraries, so the clip needs a library.

1. **Put the clip in its own folder**, with nothing else in it, e.g. `/mnt/tank/jellyfin-intros/intro.mp4`.
   Use a format your clients can play directly (H.264/AAC in MP4 is the safest choice) so the intro does
   not need transcoding.
2. **Make the folder visible to Jellyfin.** With Docker, mount it read-only:

   ```yaml
   # docker-compose.yml
   services:
     jellyfin:
       volumes:
         - /mnt/tank/jellyfin-intros:/intros:ro
   ```

   or `-v /mnt/tank/jellyfin-intros:/intros:ro` with `docker run`. Inside the container the clip is then
   `/intros/intro.mp4` — that is the path Jellyfin sees and the one the plugin needs.
3. **Add a library for it:** Dashboard → **Libraries** → **Add Media Library**, content type
   **Home Videos** (*Home videos and photos*), folder `/intros`. You can switch off metadata downloaders
   and other extras for it. Let the scan finish.
4. **The library is hidden automatically.** At startup (and for every newly created user) the plugin adds
   the intro library to each user's *My Media* and *Latest* exclusions, so it does not clutter home screens.
   Users keep access to it — the clip must stay playable. As a safety measure the library is **not**
   hidden if it holds anything besides the clip. Users can un-hide it again under Settings → Home.

## Configure

Dashboard → **Plugins** → **Movie Intro** (or **My Plugins** → Movie Intro → Settings). Click **Save** when
done; changes apply to the next playback, no restart needed (except for hiding the library, see
[Troubleshooting](#troubleshooting)).

### General and intro clip

| Setting | Default | Meaning |
|---|---|---|
| **Enabled** | on | Master switch. When off, no intro is played anywhere. |
| **Choose a video from your libraries** | — | Dropdown with the videos (`Video` items, e.g. from a *Home Videos* library) the server knows about. Picking one fills in the path below. |
| **Also list movies and music videos** | off | Adds movies and music videos to the dropdown, in case your clip lives in such a library. Only affects the dropdown, not saved. |
| **Intro clip path** | empty | Full path of the clip **as seen by the Jellyfin server** (inside the container with Docker), e.g. `/intros/intro.mp4`. You can type it by hand; the dropdown follows. If the saved path is not found in the libraries, it shows as `(current) <path>`. While empty, no intro is played. |

### Movies

| Setting | Default | Meaning |
|---|---|---|
| **Play the intro before movies** | on | Turns intros for movies on or off. |
| **Apply to all movies** | off | Every movie gets the intro; the list below is ignored (and hidden). |
| **Movie item IDs** | empty | Movies that get the intro when *Apply to all movies* is off. One ID per line (commas, semicolons and spaces also separate IDs); text after `#` is a comment. |
| **Find a movie / Search / Add to list** | — | Searches your movies by title. *Add to list* appends `<id> # <title (year)> [Movie]` to the list, skipping IDs that are already there. |

### Series (episodes)

| Setting | Default | Meaning |
|---|---|---|
| **Play the intro before series episodes** | off | Turns intros for episodes on or off. |
| **Which episodes** | Every episode | **Every episode**; **Only the first episode of each season** — the episode with the lowest episode number in its season; **Only the first episode of the series** — the lowest season/episode number across the whole series. "First" is judged among the episodes that actually exist in your library (missing/virtual episodes are ignored). Specials (season 0) never count as first. |
| **Apply to all series** | off | Every series is eligible; series and season IDs in the list are not needed. |
| **Series, season or episode IDs** | empty | Same format as the movie list. A listed **series** or **season** makes its episodes eligible, filtered by *Which episodes*. A listed **episode** always gets the intro, whatever *Which episodes* says (also when *Apply to all series* is on). |
| **Find a series, season or episode / Search / Add to list** | — | Searches series, seasons and episodes. Results look like `Show S01E02 – Title [Episode]`, `Show – Season 1 [Season]` or `Show (2019) [Series]`. |

Example list (both lists use this format):

```text
# comments start with #
5c1f0a7e9d2b4e6f8a1c3d5e7f9b0a2c # Some Show (2019) [Series]
0b9e8d7c6f5a4b3c2d1e0f9a8b7c6d5e # Other Show – Season 2 [Season]
1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d, 6d5c4b3a2f1e0d9c8b7a6f5e4d3c2b1a
```

## Client requirements

- Intros only play on clients with **cinema mode** switched on. In the web client: user menu →
  **Settings** → **Playback** → **Cinema mode** (on by default). Other apps have their own setting, and some
  apps do not support cinema-mode intros at all.
- Intros are **not** played when you **resume** a partially watched item, only when playback starts from
  the beginning.
- Whether the intro plays before an episode that starts through **auto-play next episode** (or from a
  queue/playlist) depends on the client: some clients ask the server for intros for every item they start,
  others only for the item you start yourself. If you only want one intro per binge, *Only the first
  episode of each season* or *of the series* avoids repeated intros either way.

## Examples

**Every movie, no episodes**
Play the intro before movies: on · Apply to all movies: on · Play the intro before series episodes: off.

**First episode of each season, all shows**
Play the intro before series episodes: on · Which episodes: *Only the first episode of each season* ·
Apply to all series: on.

**One show, every episode**
Play the intro before series episodes: on · Which episodes: *Every episode* · Apply to all series: off ·
search the show, select the `[Series]` result and *Add to list*.

**Once per show**
Play the intro before series episodes: on · Which episodes: *Only the first episode of the series* ·
Apply to all series: on (or list the shows you want).

**Only one season of a show**
Play the intro before series episodes: on · Which episodes: *Every episode* (or *first episode of each
season* for just its first episode) · Apply to all series: off · search the show, select the `[Season]`
result and *Add to list*.

## Finding item IDs

- Easiest: use the **search helpers** on the settings page.
- In the web client, open the movie, series, season or episode; the ID is the `id=` part of the address,
  e.g. `…/web/#/details?id=5c1f0a7e9d2b4e6f8a1c3d5e7f9b0a2c&serverId=…`.
- IDs with or without dashes are both accepted.

## Troubleshooting

**The clip is missing from the dropdown.**
The library holding it has not been scanned yet (Dashboard → Libraries → ⋮ → Scan library), the clip is in
a movie or music-video library (tick *Also list movies and music videos*), or your admin account has no
access to that library. You can always type the path by hand.

**No intro plays.**
Check in this order: *Enabled* is on; *Intro clip path* is set and saved; movies/episodes are switched on
and the item is covered by *Apply to all* or the list (and, for episodes, by *Which episodes*); cinema
mode is on in the client; you are not resuming. Then look at the server log (Dashboard → Logs): the plugin
logs `Serving intro … before …` for every intro it serves, and
`Intro clip … is not in any library yet` when the path does not match a library item.

**The path looks right but the clip is "not in any library" (Docker).**
The path must be the one *inside* the container (`/intros/intro.mp4`), not the host path
(`/mnt/tank/jellyfin-intros/intro.mp4`). Pick the clip from the dropdown to get the exact path Jellyfin
uses, and make sure the volume is mounted and the library is scanned.

**The intro library is still visible on the home screen.**
Hiding happens shortly after Jellyfin starts (retried for a few minutes while libraries load) and when a
user is created. After setting the clip path for the first time, **restart Jellyfin** once. The library is
deliberately not hidden if it contains more than the clip. Users who un-hid it in their home-screen
settings keep it visible until the next restart.

**The intro does not play when continuing a movie or episode.**
Expected: Jellyfin does not play intros when resuming.

## Build

Needs Docker (or a local .NET 10 SDK):

```sh
docker run --rm -v "$PWD":/src -w /src/Jellyfin.Plugin.MovieIntro mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish -c Release -o /src/out
```

The plugin is `out/Jellyfin.Plugin.MovieIntro.dll`; install it together with `meta.json` as described in
[Install manually](#manually).

## Release

Publish a GitHub release with a tag like `v0.2.0.0` (four numbers). The `Release` workflow builds the
plugin with that version, attaches `movie-intro_<version>.zip` to the release and adds the version — with
the release notes as changelog — to `manifest.json` on `main`, so the plugin repository offers the update.
Do not edit `manifest.json` by hand. To rebuild an existing release, run the workflow manually
(Actions → Release → Run workflow) and enter its tag.

## Uninstall

1. Dashboard → **Plugins** → **My Plugins** → Movie Intro → **Uninstall** (or delete the
   `Movie Intro_<version>` folder from `<jellyfin config>/data/plugins/`) and restart Jellyfin.
   To pause intros without uninstalling, untick *Enabled* instead.
2. Remove the intro library (Dashboard → Libraries) and, with Docker, the `/intros` volume.
3. Optionally remove the repository entry under Dashboard → Plugins → Repositories.
