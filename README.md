# Jellyfin Movie Intro

Plays a short intro clip before movies and/or series episodes, using Jellyfin's built-in cinema-mode intro
mechanism (`IIntroProvider`). Your media files are never modified. Targets Jellyfin 12.1 (`net10.0`).

## Features

- **Pick the clip from a dropdown** on the settings page. It lists the video files in `/intros` inside the
  container, read straight from disk, so new and renamed files show up immediately. Only files in `/intros`
  can be chosen (the server refuses anything else).
- **Two switches:** play before movies, play before series episodes. Every movie / every episode gets it.
- **Keeps the intro library up to date:** files that Jellyfin has not scanned yet trigger a scan of the
  intro library automatically, and there is a *Rescan /intros* button.
- **Hides the intro library** from every user's home screen, side menu and "Latest" rows, while keeping the
  clip playable.

## Install

### From the plugin repository (recommended)

1. Dashboard → **Plugins** → **Repositories** → **+** and add
   `https://raw.githubusercontent.com/DRIgnazGortngschirl/jellyfin-plugin-movieintro/main/manifest.json`
   (any name, e.g. *Movie Intro*).
2. Dashboard → **Plugins** → **Catalog**, install **Movie Intro**.
3. Restart Jellyfin.
4. Continue with [Prepare the intro folder](#prepare-the-intro-folder) and [Configure](#configure).

### Manually

1. Download `movie-intro_<version>.zip` from the
   [releases page](https://github.com/DRIgnazGortngschirl/jellyfin-plugin-movieintro/releases), or
   [build it yourself](#build).
2. Create `<jellyfin config>/data/plugins/Movie Intro_<version>/` (e.g.
   `/config/data/plugins/Movie Intro_0.3.0.0/` in the official Docker image) and put
   `Jellyfin.Plugin.MovieIntro.dll` and `meta.json` in it.
3. Restart Jellyfin. The plugin appears under Dashboard → **Plugins**.

## Prepare the intro folder

The clip must be in **`/intros` inside the container** and in a Jellyfin library.

1. **Put your clip(s) in a folder of their own** on the host, e.g. `/mnt/tank/jellyfin-intros/intro.mp4`.
   Use a format clients play directly (H.264/AAC in MP4 is the safest) so the intro needs no transcoding.
   You can keep several clips there and switch between them on the settings page.
2. **Mount it as `/intros`**, read-only:

   ```yaml
   # docker-compose.yml
   services:
     jellyfin:
       volumes:
         - /mnt/tank/jellyfin-intros:/intros:ro
   ```

   or `-v /mnt/tank/jellyfin-intros:/intros:ro` with `docker run`. Without Docker, create `/intros` on the
   server (or a symlink to your folder).
3. **Add a library for it:** Dashboard → **Libraries** → **Add Media Library**, content type
   **Home Videos** (*Home videos and photos*), folder `/intros` — nothing else. You can switch off metadata
   downloaders for it.
4. **The library is hidden automatically.** Any library whose folders are all inside `/intros` is added to
   every user's *My Media* and *Latest* exclusions — at startup, when a user is created and whenever you open
   the plugin's settings page. Users keep access, so the clip stays playable. Users can un-hide it under
   Settings → Home; it is hidden again on the next restart.

## Configure

Dashboard → **Plugins** → **Movie Intro**. Click **Save**; changes apply to the next playback.

| Setting | Default | Meaning |
|---|---|---|
| **Intro clip** | none | The clip to play, chosen from the video files in `/intros`. *— no intro —* turns intros off. Files Jellyfin has not scanned yet are marked *(not scanned yet)*; a scan starts automatically and the list refreshes. A saved clip that no longer exists (renamed or deleted) is shown as *(missing)* — pick the file again. |
| **Rescan /intros** | — | Scans the intro library now, like *Scan library* in the dashboard. |
| **Play before movies** | on | Every movie gets the intro. |
| **Play before series episodes** | off | Every episode gets the intro. |

## Client requirements

- Intros only play on clients with **cinema mode** on. Web client: user menu → **Settings** →
  **Playback** → **Cinema mode** (on by default). Other apps have their own setting; some do not support
  cinema-mode intros at all.
- Intros are **not** played when you **resume** a partially watched item.
- Whether the intro plays before an episode started by **auto-play next episode** depends on the client.

## Updating

Plugin updates are not installed automatically. When a new version is released:

1. Dashboard → **Plugins** → **Movie Intro**; in the version list, expand the new version and click
   **Install**.
2. Restart Jellyfin.

The dashboard caches the catalog for up to 15 minutes. If a new version is not listed yet, reload the
browser tab (Ctrl+Shift+R).

## Troubleshooting

**The dropdown is empty or a new/renamed file is missing.**
Check the hint under the dropdown: `/intros` must exist in the container (volume mounted) and contain video
files. The list is read from disk every time the page opens, so reopen the page after adding files.

**A file is "(not scanned yet)" for a long time.**
There is no library for `/intros`, or the library also contains folders outside `/intros` (then the plugin
does not treat it as the intro library). Add a *Home Videos* library with only `/intros`, then click
*Rescan /intros*. Jellyfin's own real-time monitoring often misses changes on bind mounts and network shares;
the plugin's scan does not depend on it.

**No intro plays.**
Check: a clip is selected and saved; *Play before movies / series episodes* is on; cinema mode is on in the
client; you are not resuming. Then look at the server log (Dashboard → Logs): the plugin logs
`Serving intro … before …` for each intro it serves, and a warning when the clip is missing, outside
`/intros`, or not scanned yet.

**The intro library is still visible on the home screen.**
Open the plugin's settings page once (that hides it for all users) or restart Jellyfin. Make sure the
library contains only `/intros`.

## Build

Needs Docker (or a local .NET 10 SDK):

```sh
docker run --rm -v "$PWD":/src -w /src/Jellyfin.Plugin.MovieIntro mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish -c Release -o /src/out
```

The plugin is `out/Jellyfin.Plugin.MovieIntro.dll`; install it together with `meta.json` as described in
[Install manually](#manually).

## Release

Publish a GitHub release with a tag like `v0.3.0.0` (four numbers). The `Release` workflow builds the plugin
with that version, attaches `movie-intro_<version>.zip` to the release and adds the version — with the
release notes as changelog — to `manifest.json` on `main`. Do not edit `manifest.json` by hand. To rebuild an
existing release, run the workflow manually (Actions → Release → Run workflow) and enter its tag.

## Uninstall

1. Dashboard → **Plugins** → Movie Intro → **Uninstall** (or delete the `Movie Intro_<version>` folder from
   `<jellyfin config>/data/plugins/`) and restart Jellyfin. To pause intros instead, select *— no intro —*
   or untick both switches.
2. Remove the intro library (Dashboard → Libraries) and the `/intros` volume.
3. Optionally remove the repository entry under Dashboard → Plugins → Repositories.
