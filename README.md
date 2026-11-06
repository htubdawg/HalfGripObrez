# Mosin Obrez for Half Grip

A compact, sawed-off Mosin for Half Grip. **0.1.0 is an initial prototype and has not been playtested.**

![Generated Obrez artwork](docs/artwork.png)

The Obrez is a separate **RaceSpec gun item**, saved as `obrez.mosin`. It occupies the SMG gun slot when equipped. Other guns keep their normal behavior.

- Five rounds per loading pack, three packs at race start.
- 65 damage, 1.5 degrees of inaccuracy, and 1.4 times the revolver's recoil by default.
- Pull and release the bolt between shots. Spent brass ejects when the bolt is pulled.
- The weapon lowers a quarter second after firing so the bolt is accessible.
- Short wood-and-steel pixel artwork, a separate bolt handle, loading packs, shop art, and a held mesh extruded from the generated pixels.

## Install

Requires **BepInEx**, **HalfGripTools 1.0.6+**, and Half Grip **0.419**. Install the dependencies using the [HalfGripMods Installer](https://github.com/4l3x4nd3r0/HalfGripMods).

For manual installation, copy the entire `Obrez` folder into:

```text
<Half Grip>/BepInEx/HalfGripMods/Obrez/
    mod.json
    icon.png
    src/
    sprites/
```

HalfGripTools compiles the source on launch. Obtain and equip **Mosin Obrez** through the normal gun item/shop system; this mod does not give it to every loadout. Its availability follows the game's item rolls.

## Controls

1. Grab a loading pack from the ammo rack and insert it into the gun's existing loading zone.
2. Grab the bolt, pull it fully back, and release it to chamber a round.
3. Hold the gun body with the left mouse button to aim. Right-click to fire.
4. After the gun lowers, pull and release the bolt again. Repeat until the pack is empty.
5. Remove the empty pack, insert a full one, and cycle the bolt.

The usual game hand bindings apply. No additional keys or scope controls are registered.

## Prototype limits

This first pass uses the game's removable SMG magazine docking as a **five-round loading pack**. It does not yet model an internal Mosin magazine, individual cartridge insertion, stripper-clip removal, or bolt-handle rotation. The body and bolt artwork are repositioned, while the loading zone and body grab area retain the stock layout. Their alignment needs checking in the game. The held weapon uses the game's thin extruded sprite style.

Shot damage, stat modifiers, hit detection, effects, and shot signals use the existing game gun code. The Obrez borrows the revolver's firing sound when available. The opening ceasefire and open-bolt firing are blocked. The game's infinite-ammo cheat keeps its normal behavior, including bypassing manual reloads.

Multiplayer behavior has not been verified. This prototype adds no custom replication. Keep the same mod installed on all peers when testing it.

## Configuration

Settings are in `BepInEx/config/halfgrip.obrez.cfg`: `Damage`, `Inaccuracy`, `RecoilMultiplier`, and `LoadingPacks`. Restart the game after changing them; item stats are set when the catalog is created.

## Development

The layout follows [jMods](https://github.com/htubdawg/jMods): a HalfGripTools source mod, a stable item ID, bounded BepInEx settings, scoped Harmony hooks, and cleanup when mods are disabled. The [Sniper Rifle](https://github.com/4l3x4nd3r0/HalfGripMods/tree/main/mods/SniperRifle) was consulted for item registration and sprite-to-held-mesh conventions. This implementation does not copy its source or art and does not depend on that mod.

All original art is generated with Python 3's standard library:

```sh
python3 tools/make_assets.py
python3 tools/package.py
```

The generated PNGs are included so players need no Python installation. `dist/HalfGripObrez-0.1.0.zip` contains the installable mod folder, this README, and the documentation. No game assemblies are included.

To compile-check with .NET 8 and local, private game references:

```sh
dotnet build tools/Check.csproj -c Release -p:GameDir=/path/to/HalfGrip -p:ToolsDll=/path/to/HalfGripTools.dll
```

On Windows, HalfGripTools' own `tools/check-mods.ps1` can also check the `Obrez` folder. Do not commit or redistribute game DLLs.

See [validation and playtest checklist](docs/VALIDATION.md) for what has and has not been checked.
