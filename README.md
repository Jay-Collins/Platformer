# Platformer

A work-in-progress 3D platformer built with Unity and C#. This repository contains the gameplay source, scenes, prefabs, input bindings, animation controllers, rendering configuration, and editor tools. Art and audio are intentionally excluded.

## Gameplay and systems

- Camera-relative movement with fixed-timestep physics and an interpolated follow camera.
- Variable-height jumping and hazard knockback.
- Health, spare lives, checkpoints, and respawn points.
- Stars, healing pickups, and extra lives.
- Idle, running, jumping, falling, landing, and death animation states.
- A title screen with keyboard/mouse navigation and an asynchronous transition into gameplay.
- UI feedback and audio playback coordinated by dedicated components.

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/Scripts/Player` | Movement and animation |
| `Assets/Scripts/Managers` | Player state, input, UI, and sound |
| `Assets/Scripts/Camera` | Camera follow and respawn snapping |
| `Assets/Scripts/Collectables`, `Hazards`, `Spawn` | Pickups, hazards, and checkpoints |
| `Assets/Scripts/UI` | Title screen and transition effects |
| `Assets/Scenes` | Title screen and gameplay scene |
| `Assets/Prefabs` | Configured gameplay objects |
| `Assets/Editor` | Animation setup, title-screen generation, and validation tools |
| `Assets/Settings`, `ProjectSettings`, `Packages` | Unity and rendering configuration |

`PlayerInputActions.cs` is generated from the accompanying `.inputactions` file. Make binding changes in the input asset and regenerate the wrapper.

## Opening the project

The project uses **Unity 6000.0.42f1**, Universal Render Pipeline **17.0.4**, and Input System **1.13.1**. Package versions are recorded in `Packages/manifest.json` and `Packages/packages-lock.json`.

This checkout includes the scene structure but is **not a complete playable distribution without the omitted content**. Expect missing visual/audio references until those dependencies are restored or replaced.

1. Open the project using Unity Hub and the editor version above. Allow Package Manager to restore dependencies.
2. Restore your own copies of the art/audio or replace the missing references. The local project uses KayKit Adventurers 1.0 EXTRA, KayKit Platformer Pack 1.0 EXTRA, and KayKit Character Animations 1.1, plus UI and audio content under `Assets/Assets`.
3. Restore the TextMesh Pro fonts and sprite resources. The existing UI uses Liberation Sans and Vipnagorgialla. TMP Essential Resources can supply the standard TMP resources; custom fonts must be restored or replaced separately.
4. Preserve the original `.meta` GUIDs when restoring content to reconnect existing references. Replacement assets or imports with new GUIDs require reassignment in scenes, prefabs, materials, and controllers.
5. After restoring the animation source pack, `Tools > Player > Rebuild Animations` creates the player clips and controller. This overwrites that controller's states. Check the menu character's Idle clip reference if clips were recreated with new GUIDs.
6. Open `Assets/Scenes/TitleScreen.unity` and enter Play Mode. Build Settings lists the title screen first, followed by `SampleScene`.

Controls: **WASD** to move; **Space** to jump (hold for a higher jump); **Enter** to activate the selected menu button. Arrow keys navigate the menu.

## Included and excluded content

Scenes, prefabs, material definitions, physics materials, animation controllers, scripts, input assets, and their Unity metadata are included. Their references to missing art are preserved.

Models, textures, images, font/atlas assets, audio, video, source art packs, and animation clips derived from those packs are excluded through `.gitignore`. Unity caches, temporary validation projects, builds, logs, and editor-specific files are also excluded.

DOTween code/binaries and its original readme are included as a dependency; its editor artwork is excluded. DOTween is by Daniele Giardini / Demigiant and retains its own [license](https://dotween.demigiant.com/license.php). TextMesh Pro shader/resource configuration is retained; its font and sprite artwork is omitted. These dependencies and the KayKit content are third-party work.

## Validation and current status

With the full local content restored, the editor exposes gameplay and title-screen validation commands under `Tools > Player`. These exercise title activation/loading, movement, animation transitions, short jumps, knockback, and movement-speed checks at requested 30/144 FPS limits.

The validation scripts currently simulate parts of death/respawn by toggling movement and repositioning the player. They do not yet cover the complete health/lives/game-over sequence. There are also known follow-up tasks around multi-collider grounding, knockback recovery without leaving the ground, and returning from game over. This is an actively developed prototype.
