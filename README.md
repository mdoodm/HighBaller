<h1 align="center">HighBaller</h1>

<p align="center">
Every counteroffer priced to land, every deal booked and handed over for you. Works on its own, nobody else needs it.
</p>

<p align="center">
<img alt="Schedule I 0.4.7f12" src="https://img.shields.io/badge/Schedule%20I-0.4.7f12-e9e6df?labelColor=1a1e28">
<img alt="IL2CPP and Mono builds" src="https://img.shields.io/badge/builds-IL2CPP%20%2B%20Mono-e9e6df?labelColor=1a1e28">
<img alt="MelonLoader 0.7" src="https://img.shields.io/badge/MelonLoader-0.7.x-e9e6df?labelColor=1a1e28">
<img alt="GPL-3.0 licence" src="https://img.shields.io/badge/licence-GPL--3.0-d95fc8?labelColor=1a1e28">
</p>

<p align="center">
<b><a href="https://github.com/mdoodm/HighBaller/releases/latest">Download the latest release</a></b> or read <a href="#install">how to install</a>
</p>

---

## What you get

In Schedule I, every counteroffer is a guess, and every deal means a phone call, a time slot and a handover screen. HighBaller does the maths and, if you want, the busywork too.

- **Know before you send.** The counteroffer screen shows the customer's maximum spend and the odds of acceptance, using the same calculation the game does.
- **The best price, filled in.** HighBaller sets the highest price that still clears your minimum success chance (98% by default). It can also raise the quantity while the price keeps climbing, and round it to the nearest 5.
- **Counteroffers on autopilot.** Turn on auto counteroffer, and HighBaller sends the optimised counteroffer the moment a customer messages you.
- **Deals book themselves.** Accepted deals are scheduled in the time window you pick. Too close to the cutoff? HighBaller moves the deal to the next window.
- **Handovers in one step.** HighBaller picks the right jars and baggies from your inventory and completes the deal. If nothing fits, you get the normal handover screen.
- **Price your products properly.** The Products app gets a Sales Information panel with potential customers, average order, per-unit price and appeal. Price optimisation adds the optimal price, the expected revenue and the units you'll need.

## In your phone

**Messages.** Open a counteroffer and HighBaller shows the customer's maximum spend and your chance of success, updating as you change the price or quantity.

**Products.** Select a product to open **Sales Information**. Drag it by the title bar, resize the text with **+** and **−**, and close it with **X**. Change the asking price and the panel recalculates once you stop clicking.

**Contacts.** Select a customer to open **Contact Information**: their spending limits, next order days, dealer and product preferences.

> [!NOTE]
> In 0.4.7f12, the game's own asking-price **+/−** buttons in the Products app don't respond to mouse clicks. HighBaller fixes them, so each click moves the price by $1.

## Install

You need **Schedule I 0.4.7f12**, on either Steam branch.

1. Install <a href="https://github.com/LavaGang/MelonLoader/releases/latest"><b>MelonLoader v0.7.x</b></a> into the game folder. Run the game once so it can set up, then exit.
2. Download the DLL for your branch from the [latest release](https://github.com/mdoodm/HighBaller/releases/latest) and put it in `Schedule I\Mods\`:

   | Steam branch | DLL |
   |---|---|
   | Default (most players) | `HighBaller-IL2CPP.dll` |
   | `alternate` (Mono) | `HighBaller-Mono.dll` |

   Put in **only one**. Both builds have the same name, so with both in `Mods` MelonLoader loads neither (see [Good to know](#good-to-know)).
3. Launch the game and load a save. The panels and the counteroffer readout work right away. The automatic features start off, and you turn them on in [Settings](#settings).

To update, replace the DLL. Your settings carry over, and both builds share them.

## Settings

Open `UserData\MelonPreferences.cfg` after the first launch and change the values under the `[HighBaller_IL2CPP...]` sections (the Mono build uses them too). **Close the game first:** MelonLoader rewrites this file when the game exits, and your edits are lost.

<details>
<summary><b>All settings</b></summary>

**Counteroffers**

| Setting | Default | What it does |
|---|---|---|
| `MinSuccessProbability` | `0.98` | Lowest acceptance chance the optimiser will settle for |
| `AutoCounterOfferEnabled` | `false` | Send the optimised counteroffer automatically |
| `IncreaseQuantityEnabled` | `false` | Raise the quantity while the best price keeps going up, up to the customer's maximum spend |
| `RoundQuantityToNearest5` | `false` | Round the quantity to a multiple of 5 (at least 5). If that would drop below `MinSuccessProbability`, the original quantity is kept |
| `CounterOfferDarkModeEnabled` | `true` | Dark background for the counteroffer readout |

**Deals**

| Setting | Default | What it does |
|---|---|---|
| `AutoContractAcceptedEnabled` | `false` | Schedule accepted deals automatically |
| `AutoDealWindow` | `LateNight` | Time window for auto scheduling. `Current` moves to the next window once the current one is too close to its cutoff |
| `AutoHandoverEnabled` | `false` | Pick packages and complete handovers automatically |
| `DelayAutoResponseDuringSave` | `true` | Wait for a save to finish before automatic actions |

**Products**

| Setting | Default | What it does |
|---|---|---|
| `PriceOptimizationEnabled` | `false` | Add optimal price, expected revenue, units needed, no-loss price and max markup to Sales Information |

**Other**

| Setting | Default | What it does |
|---|---|---|
| `S1APINPCHandling` | `Include` | How to treat custom S1API NPCs that have no customer data: include them or leave them out |
| `DebugModeEnabled` | `false` | Write detailed lines to `MelonLoader\Latest.log`. Otherwise only warnings and errors are logged |

</details>

## Good to know

- Only you need the mod. Customers, deals and prices are all handled on your side.
- Use the DLL for your branch, and swap it when you switch branches. With the wrong one, the log says `'HighBaller v2.7.12' is incompatible` and nothing loads.
- Don't keep both DLLs in `Mods`. Unlike some mods, HighBaller can't pick the right one: both builds share a name, and MelonLoader loads neither.
- Auto counteroffer doesn't change the product the customer asked for. It only sets the quantity and price.
- The odds shown are the game's own acceptance formula, so a 98% offer can still occasionally be turned down.
- If a custom NPC has no customer data, HighBaller won't open a handover for them, rather than throw errors.

## Build from source

The project compiles against your own game install, so no game files are stored here.

```sh
cd src
dotnet build -c IL2CPP -p:GameDir="/path/to/Schedule I"   # default branch, .NET 6
dotnet build -c Mono   -p:GameDir="/path/to/Schedule I"   # alternate branch, netstandard2.1
```

Run the game once on the branch you're building for first (the IL2CPP build needs `MelonLoader/Il2CppAssemblies`). You can only build the Mono configuration while the game is on the Mono branch. `GameDir` defaults to `~/.local/share/Steam/steamapps/common/Schedule I`. The DLL goes to `bin/<Config>/<tfm>/`, and the build never writes to your game folder.

Found a bug? [Open an issue](https://github.com/mdoodm/HighBaller/issues/new) and attach `MelonLoader\Latest.log` with `DebugModeEnabled = true`.

## Credits

HighBaller is built on the work of these people. Their code is still part of this mod.

| Who | Contribution |
|---|---|
| **[xyrilyn](https://github.com/xyrilyn)** | Created [Deal Optimizer](https://github.com/xyrilyn/Deal-Optimizer-Mod): counteroffer evaluation, max-spend pricing, and the IL2CPP and Mono versions |
| **zocke1r / Dom** ([fork](https://github.com/Dominik-O22/Deal-Optimizer-Mod)) | Reworked price calculation, max-spend edge cases, the 98% threshold and product appeal |
| **SFox** and **Virtunerd** | HighBaller 2.7.x ([Nexus Mods](https://www.nexusmods.com/schedule1/mods/1786)): auto counteroffer, auto scheduling, auto handover, and the Sales and Contact panels |
| **k0Mods** ([k073l](https://github.com/k073l)) | Reconstructed the HighBaller 2.7.10 source from the published DLL, restructured it into the current layout, and added 0.4.7f6 compatibility in 2.7.11. This version builds directly on that work |

The full history from xyrilyn's first commit is kept in this repository. This update was made with AI assistance.

## What's changed in 2.7.12

This version builds on k0Mods' 2.7.11. What's different:

**Game version**
- Updated for **Schedule I 0.4.7f12**, which split part of the game into a new `ScheduleOne.Core` assembly. 2.7.11 targets 0.4.7f6.
- IL2CPP and Mono build from one project, against your own game install. 2.7.11's build scripts wiped the game's `Mods` folder and relaunched the game after every build, and they've been removed.

**Fixes**
- **Counteroffer odds are accurate.** 2.7.11 rated customer enjoyment with a different formula from the one the game uses to accept a counteroffer. As a result, offers shown at 98% were sometimes refused. HighBaller now uses the game's own formula.
- **Handovers open again.** In 0.4.7f12, 2.7.11 blocked the handover screen for every deal, so neither manual nor automatic handovers worked.
- **Asking-price +/− buttons work.** In 0.4.7f12, the game's own price buttons in the Products app ignore mouse clicks. HighBaller connects them, so each click moves the price by $1.
- **No hook on the game's price networking.** 2.7.11 hooked the network call that sends a new price to the server. The Sales Information panel now watches the selected product's price instead.

**New**
- **Expected revenue.** Price optimisation now ranks prices by the total customers would pay, rather than how many would buy. Sales Information also shows the expected revenue (and per unit) and the units you'll need at the optimal price.
- **Round to the nearest 5.** The `RoundQuantityToNearest5` setting rounds counteroffer quantities to a multiple of 5, as long as the odds stay above `MinSuccessProbability`.

### How it differs from Deal Optimizer 1.4.0

xyrilyn kept developing [Deal Optimizer](https://github.com/xyrilyn/Deal-Optimizer-Mod) separately after HighBaller split from it in April 2025. The two are now different mods:

| | Deal Optimizer 1.4.0 | HighBaller 2.7.12 |
|---|---|---|
| Counteroffer odds and best price | Yes | Yes, with the odds fix above |
| Auto counteroffer, auto scheduling, auto handover | No | Yes |
| Sales Information and Contact Information panels | No | Yes |
| Built for | Earlier 0.4.x (hooks the handover screen as it was before 0.4.7f12) | 0.4.7f12 |
| Code | Separate IL2CPP and Mono copies | One shared codebase |

Fixes made in Deal Optimizer after the split, such as its off-by-one corrections to the optimal price, are not part of HighBaller yet.

Released under the [GNU GPL v3.0](LICENSE), the licence of the original Deal Optimizer. This version was modified from upstream in November 2026, and the commit history records every change. If you redistribute a build, make its source available under the same licence. [MelonLoader](https://github.com/LavaGang/MelonLoader) is licensed under Apache 2.0.

This mod is not affiliated with or endorsed by TVGS or Unity Technologies.
