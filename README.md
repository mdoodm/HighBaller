# Deal-Optimizer-Mod
A MelonLoader mod that optimizes deals for the Steam game [Schedule I](https://store.steampowered.com/app/3164500/Schedule_I/)

Features:
* Automatically sets the maximum daily cash the customer has in the Offer and Counteroffer UI
* Attempts to set the product quantity that guarantees a successful deal at the maximum daily cash price
* Evaluates the current offer to determine whether it will succeed or fail (and display the probability of success if applicable)

## Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader) for the game.
2. After installing MelonLoader for the game, run the game once for MelonLoader to setup. Exit the game.
3. Download the [latest release](https://github.com/xyrilyn/Deal-Optimizer-Mod/releases/latest) and place it in `SteamLibrary/steamapps/common/Schedule I/Mods`.
    - Copy **only one** DLL: `HighBaller-IL2CPP.dll` for the default branch, or `HighBaller-Mono.dll` for the `alternate` (Mono) branch. Having both in `Mods` stops the mod from loading.
4. Run the game. The mod is enabled automatically and requires no further setup.
5. To update the mod, follow step 3 again. To remove the mod, delete it from your Mods folder.

## Building

Target game version: **Schedule I 0.4.7f12** (MelonLoader 0.7.x).

1. Install MelonLoader and run the game once so it generates `MelonLoader/Il2CppAssemblies`.
2. Build against your own game install (references are taken from it, no game files are redistributed):
    - IL2CPP (default branch, .NET 6): `dotnet build -c IL2CPP -p:GameDir="/path/to/Schedule I"`
    - Mono (`alternate` beta branch, netstandard2.1): `dotnet build -c Mono -p:GameDir="/path/to/Schedule I (Mono)"`
    - `GameDir` defaults to `~/.local/share/Steam/steamapps/common/Schedule I`.
3. Copy `bin/<Config>/<tfm>/HighBaller-<Config>.dll` into the game's `Mods` folder. The build does not touch your game folder.

## License & Credits

[MelonLoader](https://github.com/LavaGang/MelonLoader) is licensed under the Apache License, Version 2.0. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md) for the full License.

This mod is not sponsored by, affiliated with or endorsed by Unity Technologies or its affiliates.
"Unity" is a trademark or a registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
