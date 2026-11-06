# Validation for 0.2.1

## Changes prompted by 0.1.0 playtesting

The first prototype's loading packs were oversized, its bolt used the SMG return spring, and its artwork did not read as a Mosin. The revision removes packs and their sprites, introduces individual cartridge entities and a five-round internal magazine, replaces the native slide-update logic, and redraws the rifle.

## Corrections after the 0.2.0 playtest

- Garage sizing now fits the native SMG icon footprint explicitly. The previous change only addressed cockpit sprites and left `Image.SetNativeSize` using inherited pixel density.
- The visible bolt assembly is a separate moving sprite. Its hit area is attached to the sprite, not offset on the old SMG slide origin. Parts are identified by native `smg_*` keys so skin variants do not bypass setup.
- The native hand control remains unfrozen. Zero springs and zero release momentum hold the bolt after release; there is no per-frame frozen/unfrozen switch or aim redirect.
- Round/receiver proximity is measured through the Grabber's interior camera. The insertion target uses a camera ray intersecting the cartridge movement plane, preserving on-screen alignment at different cockpit depths.
- Four/five actual loose-round entities are created before the rack records its slots. Old 15-round config values clamp to five, and the rack rejects extra ammo spawns.

## Automated checks

- C# 10 / .NET Standard 2.1 compilation against the private Half Grip 0.419 assemblies, BepInEx, Harmony, and HalfGripTools 1.0.6: zero errors and zero warnings.
- `tools/MechanicsTests` compiles the actual `BoltAction.cs` used by the mod. It checks partial/full manual travel, an open bolt remaining open for 600 samples, individual loading, maximum capacity, chambering only on closure, one shot per cycle, case/live-round ejection, and interrupted/concurrent/duplicate cartridge insertion. These are ammunition-state tests, not Unity interaction tests.
- `tools/NativeBoltTests` runs the actual game `LinearObject.StepDirect` using production `BoltControl.Configure` settings, hand input, and release. The bolt opens, holds for 600 released frames, and pushes closed. It uses an uninitialized managed instance without a running Unity engine and does not test rendering or input routing.
- Presentation math tests check that PNG resolution does not grow the garage icon and that visually aligned rounds at different depths can reach the receiver.
- Metadata checks verify the native private methods and fields used by the gun/rack hooks, including the chamber setter and rack inventory references.
- Generated assets are checked for repeatability and valid PNG structure. The preview shows the cartridge at the intended relative scale.
- Install package integrity and contents checked; no game DLLs or build outputs included.

No Unity/game runtime was available to playtest 0.2.1. Compilation and state tests do not establish cockpit alignment or the behavior of the hand/physics system in the game.

## In-game checklist

1. Install 0.2.1 with HalfGripTools 1.0.6+ on Half Grip 0.419. Confirm all patches load and `obrez.mosin` appears once. Test an existing Obrez loadout save.
2. Compare gun, bolt, and cartridge size on several cars. A loose cartridge should be about one eighth of the rifle length. Check the shop icon, bolt hit area, ammo hit areas, and held muzzle alignment.
3. Pull the bolt fully back and let go. It must remain open. Repeat with a partial pull/release; it must not spring closed or coast. Manually push it fully forward to close.
4. With the bolt open, drag a single cartridge into the receiver. It should seat without moving the bolt. Load five; a sixth must remain available outside the gun. Close manually and verify only one round chambers.
5. Fire five rounds, cycling the bolt manually after every shot. Verify five total shots, one case per full opening, and no automatic feeding or automatic repeated fire.
6. Close the bolt while a cartridge is animating in; confirm the round is not consumed. Try a partial close, two cartridges in rapid succession, loading with the bolt closed, and loading a full magazine.
7. Test shots during cooldown, with partial bolt travel, with an empty chamber, and during the opening ceasefire. None should consume ammunition improperly.
8. Start with an old config containing 15 rounds: it must supply five. A fresh config must also supply five; setting four must supply four. Extra-magazine upgrades, kills, pickups and refill effects must not add rounds. Drop an unspent round and verify native recovery. Infinite-ammo debug mode remains a cheat.
9. Refit between the Obrez and a normal SMG in the same scene. Verify that native magazines, sprite sizes, slide spring, held meshes, ammo counts, and sounds are restored. Disable mods/speedrun mode while aiming and during cartridge insertion.
10. Test multiplayer with matching mod versions before claiming network compatibility. The mod preserves native shot handling and adds no custom replication.

## Remaining scope

Bolt rotation/lift, recovering a live ejected round, and firing/handling balance still need work. All cartridges are loaded individually; stripper clips are deliberately absent.
