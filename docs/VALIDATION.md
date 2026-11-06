# Validation for 0.2.0

## Changes prompted by 0.1.0 playtesting

The first prototype's loading packs were oversized, its bolt used the SMG return spring, and its artwork did not read as a Mosin. The revision removes packs and their sprites, introduces individual cartridge entities and a five-round internal magazine, replaces the native slide-update logic, and redraws the rifle.

## Automated checks

- C# 10 / .NET Standard 2.1 compilation against the private Half Grip 0.419 assemblies, BepInEx, Harmony, and HalfGripTools 1.0.6: zero errors and zero warnings.
- `tools/MechanicsTests` compiles the actual `BoltAction.cs` used by the mod. It checks partial/full manual travel, an open bolt remaining open for 600 samples, individual loading, maximum capacity, chambering only on closure, one shot per cycle, case/live-round ejection, and interrupted/concurrent/duplicate cartridge insertion. These are ammunition-state tests, not Unity interaction tests.
- Metadata checks verify the native private methods and fields used by the gun/rack hooks, including the chamber setter and rack inventory references.
- Generated assets are checked for repeatability and valid PNG structure. The preview shows the cartridge at the intended relative scale.
- Install package integrity and contents checked; no game DLLs or build outputs included.

No Unity/game runtime was available to playtest 0.2.0. Compilation and state tests do not establish cockpit alignment or the behavior of the hand/physics system in the game.

## In-game checklist

1. Install 0.2.0 with HalfGripTools 1.0.6+ on Half Grip 0.419. Confirm all patches load and `obrez.mosin` appears once. Test an existing Obrez loadout save.
2. Compare gun, bolt, and cartridge size on several cars. A loose cartridge should be about one eighth of the rifle length. Check the shop icon, bolt hit area, ammo hit areas, and held muzzle alignment.
3. Pull the bolt fully back and let go. It must remain open. Repeat with a partial pull/release; it must not spring closed or coast. Manually push it fully forward to close.
4. With the bolt open, drag a single cartridge into the receiver. It should seat without moving the bolt. Load five; a sixth must remain available outside the gun. Close manually and verify only one round chambers.
5. Fire five rounds, cycling the bolt manually after every shot. Verify five total shots, one case per full opening, and no automatic feeding or automatic repeated fire.
6. Close the bolt while a cartridge is animating in; confirm the round is not consumed. Try a partial close, two cartridges in rapid succession, loading with the bolt closed, and loading a full magazine.
7. Test shots during cooldown, with partial bolt travel, with an empty chamber, and during the opening ceasefire. None should consume ammunition improperly.
8. Drop a loose round out of the world and verify native recovery. Pick up extra ammunition; each magazine unit should supply five individual rounds. Check rack capacity, ammo upgrades, and infinite-ammo mode.
9. Refit between the Obrez and a normal SMG in the same scene. Verify that native magazines, sprite sizes, slide spring, held meshes, ammo counts, and sounds are restored. Disable mods/speedrun mode while aiming and during cartridge insertion.
10. Test multiplayer with matching mod versions before claiming network compatibility. The mod preserves native shot handling and adds no custom replication.

## Remaining scope

Bolt rotation/lift, recovering a live ejected round, and firing/handling balance still need work. All cartridges are loaded individually; stripper clips are deliberately absent.
