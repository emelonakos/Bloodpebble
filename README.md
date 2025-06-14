# Bloodpebble

![bloodpebble-banner](https://github.com/cheesasaurus/Bloodpebble/raw/main/bloodpebble-banner.png)

Bloodpebble is a modding tool for the game [V Rising](https://playvrising.com/) . It allows reloading BepInEx plugins without restarting the game.

See the [thunderstore readme](README-thunderstore.md) for information about features, how to use it, etc.

## Principles

- Bloodpebble has one job only: hot reloading plugins. It should do it well. And do nothing else.
- Plugin dependencies should be correctly resolved. Dependencies can also be hot reloaded, or already exist via the usual bepinex loading process.
- Bloodpebble should never be a dependency of other plugins. Any interfacing (e.g. opting in/out of reloads) should be done in ways that don't require a library or exposing Bloodpebble internals.
- Bloodpebble should never have a hard dependency on another plugin.
- Plugin reloading should be robust. An error with one plugin should have minimal impact on the loading of other plugins.
- Errors should be handled in a way that makes troubleshooting as easy as possible.
- When it comes to tradeoffs, let the user decide via configuration.

## History

Bloodpebble started as a fork of [Bloodstone](https://github.com/decaprime/Bloodstone), with the goal of adding dependency resolution, and simply getting merged back into the mainline.

But major game updates repeatedly exposed problems revolving around Bloodstone. Aiming to avoid those problems,  Bloodpebble ended up evolving into a different project with different objectives.

## Developer Documentation

### Project setup

1. Setup links to the bepinex libraries and game interops. See the [vendor readme](vendor/README.md) for more information.
2. Restore with `dotnet restore`

### Building
- Build with `dotnet build`
- Build more things with `dotnet publish`
  - Outputs the thunderstore package into `dist/`
  - Also outputs `Bloodpebble.dll` into `dist/`