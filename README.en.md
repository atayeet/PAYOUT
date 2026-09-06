# PAYOUT 💥

[Türkçe](README.md) · **English**

[![Unity 6](https://img.shields.io/badge/Unity-6000.4.11f1-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%202D-orange.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![C#](https://img.shields.io/badge/Language-C%23%2010.0-purple.svg?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-PC%20%2F%20Windows-lightgrey.svg?logo=windows)](https://microsoft.com/windows)
[![Genre](https://img.shields.io/badge/Genre-2D%20Top--Down%20Shooter-red.svg)](#)

> **PAYOUT** is an adrenaline-fueled, high-lethality **2D Low-Res Pixelated Top-Down Shooter** inspired by *Hotline Miami*, *Ape Out*, and classic neo-noir arcade twin-stick shooters.

---

## 📖 Table of Contents
- [About the Game](#-about-the-game)
- [Core Gameplay Mechanics](#-core-gameplay-mechanics)
- [Keybindings](#-keybindings)
- [Arsenal & Weapons](#-arsenal--weapons)
- [Reactive AI Architecture](#-reactive-ai-architecture)
- [Difficulty Settings](#-difficulty-settings)
- [Documentation](#-documentation-project-specs)
- [Technical Architecture & File Structure](#-technical-architecture--file-structure)
- [Getting Started](#-getting-started)
- [License & Copyright](#-license--copyright)

---

## 🎮 About the Game

PAYOUT is a room-clearing simulation built around speed, sharp reflexes, improvisational tactics, and uncompromising violence. Every room is a lethal puzzle; every enemy is an immediate threat that must be addressed within fractions of a second.

One bullet will kill you. When your magazine runs dry, punch your way through, throw your empty pistol straight into a guard's face to knock him flat, grab his dropped shotgun from the floor, and turn the corridor into a crimson bloodbath.

---

## ⚡ Core Gameplay Mechanics

### 1. Decoupled Movement & Dual-Axis Aiming
The player's legs run across the world according to `WASD` input, while the upper torso independently locks toward a clamped virtual crosshair (`VirtualCursor`). You can sprint backward down a hallway while laying down continuous suppressive fire into advancing hostiles.

### 2. Tactical Look-Ahead (`Shift` Mechanic)
Holding down `Shift` (Left or Right Shift):
* Expands the maximum crosshair distance away from the player (`7m` $\rightarrow$ `12m`).
* Smoothly offsets the Cinemachine 2D camera toward the midpoint between player and cursor.
* Temporarily relaxes viewport edge clamping, allowing you to scout the next room or corner before breaching.

### 3. Melee, Stun & Brutal Finishers
* **Unarmed Punches:** When disarmed, Left Click punches by alternating between right and left fists. Punching an enemy knocks them backward (`_punchKnockbackForce`), causes them to drop their weapon, and leaves them stunned for `1.5` seconds.
* **Execution Finishers:** Approach a stunned enemy and press `Space` to enter the finisher stance. Left Click executes the target, accompanied by a bone-snapping audio cue (`Finisher-bone-break`) and violent blood burst (`FrontBlood`), instantly neutralizing them.
* **Finisher Stamina:** Performing an execution depletes your stamina bar. Stamina passively regenerates over time and replenishes with each kill (scaled by difficulty).

### 4. Weapon Throwing & Ammo Preservation
* **Right Click (Armed):** Chucks your currently equipped firearm in your aim direction. Thrown weapons act as high-speed physical projectiles that stun any enemy on impact and knock their weapons away.
* **Right Click (Unarmed):** Sweeps the immediate vicinity (`_pickupRadius = 1.5f`) and equips the nearest dropped firearm.
* **Ammo Persistence:** When a gun is thrown or dropped, its remaining bullet count is strictly preserved — no magic reloads upon picking it back up.

### 5. Reactive Environment & Physical Props
* **Physical Hinge Doors:** Slamming into doors engages motor torque (`HingeJoint2D`). The swinging door blade knocks down and stuns any enemies standing directly on the other side.
* **Proximity Lamps:** Smart ceiling fixtures equipped with entity detection and line-of-sight checks. Lamps illuminate when living entities are present and dim after a delay once vacated.

---

## 🕹️ Keybindings

| Key | Action | Description |
| :--- | :--- | :--- |
| **W, A, S, D** | Movement | Moves player legs across the 2D plane |
| **Mouse Move** | Aiming | Orients torso and guides the virtual crosshair |
| **Left Click** | Fire / Punch | Fires held gun, or alternates punches when unarmed |
| **Right Click** | Throw / Pickup | Throws held weapon (stuns foes) or picks up weapon |
| **Space** | Finisher Stance | Locks onto a stunned enemy when stamina is full |
| **Left Click (Finisher)** | Execute Finisher | Snaps enemy bones and finishes the target |
| **Left / Right Shift** | Look-Ahead | Pans camera and extends crosshair forward for scouting |
| **R** | Quick Restart | Instantly restarts current level after player death |
| **ESC** | Pause / Settings | Opens/closes pause menu and audio sliders |

---

## 🔫 Arsenal & Weapons

| Weapon | Type | Magazine | Fire Rate | Characteristic |
| :--- | :--- | :---: | :---: | :--- |
| **Pistol** | Semi-Automatic | 15 | Medium | Balanced, accurate standard-issue sidearm |
| **Shotgun** | Pump-Action | 6 | Slow | 6 pellets spread (-15° to +15°), lethal at close range |
| **Rifle** | 3-Round Burst | 30 | Fast | Devastating 3-round burst (0.1s interval) per trigger pull |
| **Minigun** | Full-Automatic | 100 | Very Fast | High-volume suppression bullet storm |

---

## 🤖 Reactive AI Architecture

Enemies navigate dynamic 2D environments via NavMesh (`NavMeshPlus`) and operate on a multi-state machine:
* **Patrol:** Patrols between room patrol points (`PatrolPoint`).
* **Alert:** Momentary reaction delay upon spotting the player (`0.15s` - `1.0s` depending on difficulty).
* **Chase:** Relentlessly pursues the player and opens fire once within shooting range (`7m`).
* **InvestigateLastSeen:** When line-of-sight is broken, searches the player's last known location.
* **SearchWeapon / Flee:** If disarmed by a punch, door, or thrown weapon, the AI sprints toward the nearest dropped gun on the floor; if no weapon is nearby, it flees in panic.

---

## ⚖️ Difficulty Settings

Balanced through `DifficultyManager` and persisted via `PlayerPrefs`:
* **Easy:** Enemy alert reaction `1.0s`, enemy fire rate `%60` slower, rapid finisher stamina recovery.
* **Normal:** Standard arcade challenge with balanced reactions.
* **Hard:** Brutal `0.15s` enemy reaction, rapid enemy fire, restricted stamina rewards.

---

## 📚 Documentation (Project Specs)

Comprehensive technical, design, and architecture documents are maintained inside the [`Docs/`](Docs/) directory:

* 📄 **[GDD.md](Docs/GDD.md) — Game Design Document:**
  Core pillars, player mechanics, weapon ballistics, full AI state diagram, interactive prop mechanics, and difficulty matrices.
* 📄 **[CONTEXT.md](Docs/CONTEXT.md) — Technical Architecture & Context:**
  Unity 6 URP 2D setup, Additive scene pipeline (`CoreScene` + `Level_X`), object pooling (`BulletPool`, `EffectPool`), physics & sorting layer indices, and complete script breakdown.
* 📄 **[CODE_STYLE.md](Docs/CODE_STYLE.md) — Coding Standards & Best Practices:**
  C# and Unity clean architecture guidelines, naming conventions, serialization standards, lifecycle safety, and zero-allocation hot paths.
* 📄 **[GEMINI.md](Docs/GEMINI.md) — AI Assistant & LLM Guidelines:**
  System instructions for AI agents working on this codebase, Unity MCP server integration guide, and checklists for adding weapons, enemies, and levels.

---

## 🏗️ Technical Architecture & File Structure

* **Additive Scene Hierarchy:** `CoreScene` remains persistent (Camera, Player, UI Canvas, Singleton managers). Levels (`Level_1`, `Level_2`, `Level_3`) load additively and unload seamlessly upon level completion.
* **Object Pooling:** `BulletPool` (projectiles) and `EffectPool` (blood splatters and wall impacts) eliminate GC allocation spikes.
* **Continuous Collision Detection (CCD):** Fast projectile linecasting (`Physics2D.LinecastAll`) prevents wall/entity tunneling.

```
PAYOUT/
├── Assets/
│   ├── Animations/           # Player, enemy, and weapon animation controllers
│   ├── Audio/                # MainMixer, ambient music, gunshot, and impact SFX
│   ├── Prefabs/              # Weapons, enemies, doors, and blood particle prefabs
│   ├── Scenes/               # CoreScene, MainMenu, Level_1, Level_2, Level_3
│   ├── Scripts/              # C# core logic and architectural classes
│   └── Sprites/              # Retro pixel-art characters, environments, and weapons
└── Docs/                     # Project design, architecture, and coding guides
    ├── CODE_STYLE.md         # C# and Unity coding standards
    ├── CONTEXT.md            # Technical infrastructure and engine context
    ├── GDD.md                # Comprehensive Game Design Document
    └── GEMINI.md             # AI developer & assistant guidelines
```

---

## 🚀 Getting Started

1. **Prerequisites:**
   * **Unity Hub** with **Unity 6000.4.11f1 (Unity 6 LTS)** installed.
   * Git (Git LFS recommended).
2. **Clone the Repository:**
   ```bash
   git clone https://github.com/atayeet/PAYOUT.git
   ```
3. **Open in Unity:**
   * Add the project in Unity Hub and launch with `Unity 6000.4.11f1`.
4. **Play the Game:**
   * Open `Assets/Scenes/MainMenu.unity` and click **Play**.
   * For direct level iteration, launch from `Assets/Scenes/CoreScene.unity`.

---

## 📜 License & Copyright
Developed and maintained by [atayeet](https://github.com/atayeet). All rights reserved.
