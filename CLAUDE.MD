# CustomMod — Captain of Industry Mod

## Project Overview

A C#/.NET Framework 4.8 mod for "Captain of Industry" by MaFi Games. Bundles several independent gameplay tweaks into a single mod using the official COI modding framework (IMod interface).

## Build & Deploy

- **Framework**: .NET Framework 4.8 (`net48`)
- **Build**: `dotnet build` or open `CustomMod.sln` in Visual Studio
- **Environment variable**: `COI_ROOT` must point to the game install (e.g. `C:\Program Files (x86)\Steam\steamapps\common\Captain of Industry`)
- **Auto-deploy**: Build copies `CustomMod.dll` + `manifest.json` to `%APPDATA%\Captain of Industry\Mods\CustomMod\`
- **Game assemblies**: Referenced from `$(COI_ROOT)\Captain of Industry_Data\Managed\` (base) and `$(COI_ROOT)\DLCs\Mafi.TrainsDlc\` (Trains DLC)
- **All references are Private=false** — do not bundle game DLLs with the mod

## Architecture

Entry point: `CustomMod.cs` implements `IMod` with 4-phase lifecycle:
1. `RegisterPrototypes` — registers `IModData` classes that modify game prototypes
2. `RegisterDependencies` — registers runtime services (e.g. `UnityPopulationScaler`)
3. `EarlyInit` / `Initialize` — unused currently

### Mod Components

| File | What it does |
|---|---|
| `FbrSteamModData.cs` | FBR: 4x water/steam, 4 TFlops computing; doubles Maintenance T3 recipe outputs |
| `ElectricTrainModData.cs` | Buffs T1/T2 electric locomotives (power, TE, speed, braking, aero, costs) |
| `WagonCapacityModData.cs` | 4x capacity for all wagon types (T1/T2 unit/loose/fluid + T2 molten DLC) |
| `UnityPopulationScaler.cs` | Scales unity production multiplier based on population above 750 |

### Prototype Modification Pattern

Game prototypes use `readonly` fields. This mod modifies them via reflection:
- `SetField<T>(obj, fieldName, value)` — sets readonly fields using `FieldInfo.SetValue`
- `SetProperty<T>(obj, propName, value)` — tries property setter first, falls back to `<PropName>k__BackingField`
- `EntityCosts` is a readonly struct — must construct a new instance and set the backing field `<Costs>k__BackingField`

## Key Game Types

- `ProtosDb` — prototype database, use `GetOrThrow<T>(id)` to fetch
- `Proto.ID` — prototype identifier; base game IDs in `Ids.*`, DLC in `IdsTrainsDlc.*`
- `Fix32` — fixed-point number used throughout the engine; cast from int/double
- `Quantity` — wrapper around int for item counts
- `Percent` — use `Percent.FromRatio(numerator, denominator)` for multiplier conversion
- `MechPower` — use `int.KwMech()` extension
- `Electricity` — use `int.Kw()` extension

## Conventions

- Namespace: `CustomMod` for all files
- Each independent tweak is a separate `IModData` class registered in `CustomMod.cs`
- Log with `Mafi.Log.Info(...)` / `Mafi.Log.Warning(...)` / `Mafi.Log.Error(...)`
- DLC-dependent code wrapped in try/catch for graceful degradation
- The `decompiled\` folder contains reference decompiled game code — excluded from compilation via csproj

## Wagon Capacity Design Decision

Wagon capacity is 4x but mass is intentionally unchanged (same empty/full mass as vanilla). This is a deliberate balance choice, not an oversight.
