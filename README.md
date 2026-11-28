<p align="center"><img src="docs/banner.png" alt="Pixel art: two farmers tend rows of crops beside a farmhouse and a chest" width="100%"></p>

# stardew-mods

Two SMAPI mods for co-op Stardew Valley, in C#:

- **Co-op Longer Days** (version 1.2.1): a slower day clock, with the host's setting sent to every player.
- **Hired Hands Co-op Bridge** (version 1.0.0): lets the Hired Hands mod 1.2.1 run in multiplayer, with the host in charge.

## Run it

You need the game, SMAPI 4 and the .NET 6 SDK. Set the game paths in each `.csproj` (they point to a macOS Steam install), run `dotnet build` in the mod's folder, and copy the folder into the game's `Mods` folder. Each mod's README has the details and limits.

## Build and test

```sh
make check    # checks the manifests, and the C# formatting if dotnet is installed
```

The mods need the game's DLLs to build, so CI runs only `make check`. This repository holds no game files. Status: [STATUS.md](STATUS.md). MIT licence.
