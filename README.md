# HighBaller

A [MelonLoader](https://github.com/LavaGang/MelonLoader) mod for [Schedule I](https://store.steampowered.com/app/3164500/Schedule_I/) that helps you price deals, counteroffer and complete handovers.

HighBaller continues **Deal Optimizer** by xyrilyn and the HighBaller versions that came after it, most recently k0Mods' 2.7.11 (see [Credits](#credits)). This version is updated for **Schedule I 0.4.7f12** on both the default (IL2CPP) and `alternate` (Mono) branches.

## Features

**Counteroffers**
- Shows the customer's maximum spend and whether your counteroffer will be accepted, with the probability of success.
- Fills in the best price, and optionally the best quantity, that still meets your minimum success probability.
- Optional **auto counteroffer**, which sends the optimised counteroffer for you.
- Optional **round quantity to the nearest 5**. If the rounded quantity would drop below your minimum success probability, the original quantity is kept.

**Deals**
- Optional **auto schedule**, which accepts the deal in a time window you choose.
- Optional **auto handover**, which picks the right packages from your inventory and completes the deal. If nothing fits, it falls back to the normal handover screen.

**Products app**
- A **Sales Information** panel for the selected product: potential customers, average order size, average payment, per-unit price and customer appeal.
- Optional **price optimisation**: optimal price, expected revenue (and per unit), units needed, no-loss price and maximum markup.
- Fixes the asking-price **+/−** buttons, which do nothing on mouse click in 0.4.7f12.

**Contacts app**
- A **Contact Information** panel with customer spending and relationship details.

## Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.x, run the game once so it can set up, then exit.
2. Copy **one** DLL into `Schedule I/Mods`:
   - `HighBaller-IL2CPP.dll` for the default branch.
   - `HighBaller-Mono.dll` for the `alternate` (Mono) branch.

   Don't put both in `Mods`. They share a name, so MelonLoader loads neither.
3. Start the game. To remove the mod, delete the DLL.

When you switch Steam branches, switch the DLL as well.

## Configuration

Settings are in `Schedule I/UserData/MelonPreferences.cfg` and are shared by both builds. Edit the file while the game is closed, because MelonLoader rewrites it when the game exits.

| Setting | Default | What it does |
|---|---|---|
| `MinSuccessProbability` | `0.98` | Lowest acceptance chance the optimiser will settle for |
| `AutoCounterOfferEnabled` | `false` | Send the optimised counteroffer automatically |
| `IncreaseQuantityEnabled` | `false` | Raise the quantity while the price keeps going up |
| `RoundQuantityToNearest5` | `false` | Round the counteroffer quantity to a multiple of 5 |
| `AutoContractAcceptedEnabled` | `false` | Schedule accepted deals automatically |
| `AutoDealWindow` | | Time window used for auto scheduling |
| `AutoHandoverEnabled` | `false` | Complete handovers automatically |
| `PriceOptimizationEnabled` | `false` | Show the price optimisation section in Sales Information |
| `CounterOfferDarkModeEnabled` | `true` | Dark background for the counteroffer readout |
| `DelayAutoResponseDuringSave` | `true` | Wait for saving to finish before auto actions |
| `S1APINPCHandling` | `Include` | How to treat custom S1API NPCs that have no customer data |
| `DebugModeEnabled` | `false` | Verbose logging to `MelonLoader/Latest.log` |

## Building

Target: **Schedule I 0.4.7f12**, MelonLoader 0.7.x. The project compiles against your own game install, so no game files are stored in this repository.

1. Install MelonLoader and run the game once on the branch you're building for. For IL2CPP, this generates `MelonLoader/Il2CppAssemblies`.
2. Build:
   ```sh
   cd src   # the folder containing HighBaller.csproj
   dotnet build -c IL2CPP -p:GameDir="/path/to/Schedule I"   # default branch, .NET 6
   dotnet build -c Mono   -p:GameDir="/path/to/Schedule I"   # alternate branch, netstandard2.1
   ```
   `GameDir` defaults to `~/.local/share/Steam/steamapps/common/Schedule I`. You can only build the Mono configuration while the game is on the Mono branch.
3. Copy `bin/<Config>/<tfm>/HighBaller-<Config>.dll` into `Mods`. The build never writes to your game folder.

## Credits

HighBaller is built on the work of these people. Their code is still part of this mod.

| Who | Contribution |
|---|---|
| **[xyrilyn](https://github.com/xyrilyn)** | Original author of [Deal Optimizer](https://github.com/xyrilyn/Deal-Optimizer-Mod): counteroffer evaluation, max-spend pricing, the IL2CPP and Mono versions, and the GPL-3.0 licence this project inherits |
| **zocke1r / Dom** ([fork](https://github.com/Dominik-O22/Deal-Optimizer-Mod)) | Refactored price calculation, max-spend edge cases, the 98% probability threshold, product appeal and the pricing rework |
| **SFox** and **Virtunerd** | HighBaller 2.7.x ([Nexus Mods](https://www.nexusmods.com/schedule1/mods/1786)): auto counteroffer, auto scheduling, auto handover, and the Sales and Contact panels |
| **k0Mods** ([k073l](https://github.com/k073l)) | Reconstructed the HighBaller 2.7.10 source from the distributed DLL, restructured it into the current layout, added 0.4.7f6 compatibility and released 2.7.11. This update builds directly on that work |
| **mdoodm** | 0.4.7f12 update (2.7.12): IL2CPP and Mono builds, counteroffer odds fix, handover fix, price-button fix, revenue figures and rounding to 5 |

The full history from xyrilyn's first commit is kept in this repository.

Assisted with AI tools.

## Licence

HighBaller is free software under the **GNU General Public License v3.0**, the same licence as the original Deal Optimizer. See [LICENSE](LICENSE). This version was modified from upstream in November 2026. The commit history records what changed and when. If you redistribute a build, you must also make its source available under the same licence.

[MelonLoader](https://github.com/LavaGang/MelonLoader) is licensed under the Apache License 2.0.

This mod is not affiliated with or endorsed by TVGS or Unity Technologies. "Unity" is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
