# Validation for 0.1.0

## Completed in the development environment

- Compiled the source as C# 10 / .NET Standard 2.1 against the supplied private Half Grip 0.419 assemblies, BepInEx, Harmony, and HalfGripTools 1.0.6: zero errors and zero warnings.
- Inspected the native gun and magazine APIs: `NAutomaticGun.Shoot` consumes the chambered round and calls `TryChamber`; `OnSlideReturned` also calls `TryChamber`. The scoped hook suppresses feeding only during a shot. Native `NMagazine.Awake` takes capacity from the fitted item.
- Generated the transparent PNG assets without external images or libraries. Checked reproducible output and inspected the artwork preview.
- Checked the distribution contents for the manifest, source, and art, with no game assemblies.

These checks establish source/API compatibility and asset/package integrity. They do not establish in-game operation. No Unity/game runtime was available for playtesting.

## In-game checks still required

1. Launch with HalfGripTools 1.0.6+ on Half Grip 0.419. Check that all Harmony patches load and the Obrez catalog item appears once.
2. Equip the Obrez. Check docked art, bolt hit area, loading zone, held mesh, muzzle position, sound, and shop icon on multiple cars.
3. Insert a full pack and cycle the bolt: four rounds should remain in the pack and one in the chamber. Fire once: no automatic second round should feed. Hold right-click: no automatic fire.
4. Cycle the bolt: eject one spent case and chamber one round. Fire all five rounds, confirming the total; test a partial pull that never reaches the back stop.
5. Try firing with the bolt open, an empty chamber, during the opening ceasefire, and during the shot cooldown. Confirm ammo and case counts remain consistent.
6. Swap a partially full pack; test removal, dropping, pickup, extra-magazine items, re-equipping on another race, and a saved loadout.
7. Exercise damage/recoil modifiers and infinite ammo. Confirm normal SMG, shotgun, revolver, and Sniper Rifle behavior when each is equipped instead.
8. Disable mods through speedrun mode while docked and while aiming. Check restoration of body sprites, ammo sprites, bolt pose/hit box, held meshes, sound, and gun settings.
9. Test multiplayer with matching mod versions before claiming network compatibility.

## Follow-up work

- Replace the removable-pack prototype with an internal five-round magazine, individual rounds, and stripper-clip loading.
- Add bolt lift/rotation and tune docking geometry from actual game captures.
- Tune spread, recoil, cost, rarity, and starting ammunition through playtesting.
