# Weapon System

A first person / third person weapon system made in Unity (URP), with a shooting range to test it.

> **Note:** Logic build mein AI ki help li gayi hai.

## Controls
| Key | Action |
|---|---|
| W A S D | Move |
| Shift | Sprint (the gun is lowered, no shooting) |
| C | Crouch |
| Space | Jump |
| Left mouse button | Fire (or slash with the knife) |
| Right mouse button | Aim down sights / look through the scope |
| R | Reload |
| B | Switch auto / semi fire |
| 1 - 6 or mouse wheel | Change weapon (1 = knife) |
| Mouse wheel while aiming | Scope zoom (sniper 4x - 12x) |
| [ and ] | Change the zero distance of a scope (50 / 100 / 200 / 300 m) |
| Shift while scoped | Hold breath (less scope sway, limited time) |
| V | First person / third person |
| T | Reset the range (score, ammo, targets) |
| Esc | Free the cursor |

## Features
- **Six weapons:** knife, pistol, SMG, assault rifle (fixed 4x scope), shotgun and sniper (variable 4x - 12x scope). Every gun is a `WeaponData` ScriptableObject, so the code is the same for all of them.
- **Real bullets:** speed and gravity, so far targets need a hold-over. Damage drops with distance.
- **Recoil, spread and bloom** that change with moving, jumping, crouching and aiming.
- **Magazine + reserve ammo** with a round in the chamber, longer reload when empty, auto reload.
- **Scopes:** round lens, duplex reticle, hold-over marks calculated from the real ballistics, range finder, zeroing, scope sway and breath hold.
- **Hit zones:** head x2.5, body x1, limbs x0.7, hit markers, bullet holes, sparks and blood.
- **Training range:** score boards (disappear for 5 s after a hit), dummies that respawn after 10 s, a moving target and pop-up targets, with a score and accuracy HUD.

## Run it
1. Unity **6000.3.25f1** (Universal 3D / URP).
2. Open `Assets/Scenes/SampleScene.unity` and press Play.