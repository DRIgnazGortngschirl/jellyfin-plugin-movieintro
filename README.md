# Jellyfin Movie Intro

Plays a short intro clip before movies via Jellyfin's cinema-mode intros (`IIntroProvider`).
Media files are never modified. Targets Jellyfin 12.1 (`net10.0`).

## Build

```sh
docker run --rm -v "$PWD":/src -w /src/Jellyfin.Plugin.MovieIntro mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish -c Release -o /src/out
```

## Release

Publish a GitHub release with a tag like `v0.1.0.0`. The `Release` workflow builds the plugin, attaches
`movie-intro_<version>.zip` to the release and adds the version to `manifest.json` on `main`.

## Install from the plugin repository

Dashboard → Plugins → Repositories → add
`https://raw.githubusercontent.com/DRIgnazGortngschirl/jellyfin-plugin-movieintro/main/manifest.json`,
then install *Movie Intro* from the catalog, restart Jellyfin and continue with step 1 and 3–4 below.

## Install manually

1. Put the intro clip in its own folder and mount it into the Jellyfin container read-only, e.g.
   `/mnt/tank/jellyfin-intros:/intros:ro`.
2. Copy `out/Jellyfin.Plugin.MovieIntro.dll` and `meta.json` to
   `<config>/data/plugins/Movie Intro_0.1.0.0/` and restart Jellyfin.
3. Add a library (type *Home Videos*) for `/intros` containing only the clip. The plugin hides it from
   every user's home screen and side menu (users keep access so the clip stays playable).
4. Dashboard → Plugins → Movie Intro: set the clip path and either enable *Apply to all movies* or list
   movie item IDs.

Clients must have cinema mode enabled (web: Settings → Playback → Cinema mode, on by default).
Intros are not played when resuming playback.

## Uninstall

Disable it on the settings page, or delete the plugin folder and restart, then remove the intro library.
