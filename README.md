# Okun: Submarine Odyssey

A 3D submarine fishing game built with C# and [Raylib-cs](https://github.com/raylib-cs/raylib-cs).

You pilot a submarine through an underwater world, steering with an aim cursor and firing a hook
at the fish swimming around you. Hooked fish fight the line, give in, and get reeled in.

The project is an early-stage prototype: the core loop (steer, lock on, hook, fight, reel in) works,
and the world around it (terrain, fish models, weapons, sound) is still being built.

## Features

- **Aim-cursor steering** - the mouse moves an aim direction in the world and the submarine turns to
  face it, with the cursor settling back onto the nose as the ship catches up
- **Hook weapon** - lock on along the cursor, fire a hook that flies out on a rope, bites into a fish,
  and reels it in; the rope tints and rattles while the fish fights
- **Fish AI** - fish wander, panic when the submarine gets close, resist when hooked, and are reeled
  in when they give up (a state machine, one class per state)
- **Real submarine model** - a `.glb` model, lit correctly as it yaws, pitches and rolls
- **Underwater atmosphere** - distance fog plus directional lighting through a custom shader
- **Particles** - bubble trail off the sub, drifting sediment, capture bursts, and a hook trail
- **HUD** - depth gauge, fish-caught counter, and a cursor that changes shape and color by mode

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) 10.0 (the project targets `net10.0`)
- [Raylib-cs](https://www.nuget.org/packages/Raylib-cs) 8.0.0 (restored automatically via NuGet)
- A GPU/driver with OpenGL 3.3 or later

## Getting Started

```bash
git clone <your-repo-url>
cd Okun
dotnet restore
dotnet run
```

**Run it from the project root.** Shaders and models are loaded by relative path
(`Resources/Shaders/...`, `Resources/Models/...`), so the working directory must be the folder that
contains `Okun.csproj`. In VS Code, the launch configuration should set `"cwd": "${workspaceFolder}"`
(press F5 to build and run).

`Okun.csproj` needs `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`: Raylib-cs exposes a model's
materials as raw pointers, and `SubmarineModel` reads them to attach the shader.

If a shader fails to compile, the game stops at startup with an error saying which shader and where to
look, rather than silently drawing nothing.

## Controls

| Input            | Action                                                                              |
|------------------|-------------------------------------------------------------------------------------|
| Mouse            | Move the aim cursor; the submarine turns to face it                                 |
| Left click / `H` | Fire the hook (homes in on a locked fish, otherwise flies straight along the aim)   |
| `C`              | Recenter the cursor on the submarine's nose                                         |
| `W` / `S`        | Move the submarine up / down                                                        |
| `A` / `D`        | Roll left / right                                                                   |
| `F1`             | Toggle fish debug colors (state colors vs. species colors)                          |
| `F2`             | Toggle the submarine bounding box                                                   |
| `Esc`            | Close the window                                                                    |

The mouse is captured on launch (locked and hidden) so aiming isn't limited by the edges of the
screen. It doesn't move a screen position directly: it moves an *aim direction* in the world, and the
cursor is that direction projected onto the screen. The aim can only lead the ship by a limited cone
(35° sideways, 20° up or down) so the cursor always stays in view. A small ring marks where the
submarine's nose is actually pointing.

The submarine cruises forward at a constant speed for now, so there is something to steer.

### Cursor modes

The cursor changes shape and color to show what's happening. Each mode is one row in the `Styles`
table in `CursorRenderer.cs`, so adding or retuning a mode doesn't touch any game logic.

| Mode            | Look                  | When                                    |
|-----------------|-----------------------|-----------------------------------------|
| `Navigate`      | white corner brackets | steering, nothing targeted              |
| `TargetLocked`  | green diamond         | a hookable fish is under the cursor     |
| `HookInFlight`  | orange spinning ring  | the hook has been fired                 |
| `HookRetracting`| grey ring, spinning backwards | the hook is winding back in; it can't be fired again until it's home |
| `FishResisting` | red pulsing crosshair | the hooked fish is fighting the line    |
| `Reeling`       | blue spinning diamond | the fish has given in and is reeled in  |

A locked or hooked fish also gets a square bracket marker so it can't be confused with the cursor.

## Camera

A third-person chase camera sits behind and above the submarine and looks a little way ahead of it,
so the sub sits low in the frame with room above it for the cursor to lead the ship. The camera banks
with the submarine's roll, about the camera's own forward axis, so the bank looks the same whichever way
the submarine is heading (`CameraMath.BankedUp`).

## How it fits together

- **Steering** - `AimCursor` holds the world-space aim and turns the ship toward it at a capped rate.
  The same aim direction is what the hook fires along, so where the cursor points is both where the
  ship is heading and where it will shoot.
- **Fish** - each `Fish` (idle fish keep their wander targets inside the world; panicking fish slide along walls instead of pinning against them) delegates to a `FishState` (`IdleSwimState`, `PanicState`, `ResistingState`,
  `CapturedState`). The only thing anything outside the fish code needs to call is `Fish.Hook()`.
  `FishManager` owns spawning, updating, drawing, lock-on queries and collection.
- **Hook** - `HookLauncher` handles the hook's flight, attachment, rope, and retraction. It only talks
  to fish through `Fish.Hook()`, so it can become `Harpoon : Weapon` when the weapon base class exists.
- **Rendering** - fish, particles and the hook are raylib immediate-mode shapes; the submarine is a
  `Model`. They need different vertex shaders (raylib supplies transforms differently for each), so
  `SceneShaders` loads two variants (`fog.vs`, `model.vs`) that share one fragment shader (`fog.fs`) and
  keeps their settings in sync. The grid and debug wireframes are deliberately drawn outside the shader.
- **Particles** - `ParticleSystem` is a pool for spawn-then-die particles (bubbles, bursts, the hook
  trail). `SedimentField` is separate because its particles wrap around the world instead of dying.

## Project Structure

```
Okun/
├── Program.cs                  # Entry point: window, camera, game loop, wiring
├── Okun.csproj                 # net10.0, Raylib-cs 8.0.0, AllowUnsafeBlocks
├── CursorRenderer.cs           # Cursor shapes/colors per mode (the Styles table)
├── HudRenderer.cs              # Depth gauge and fish counter
├── Controls/
│   └── AimCursor.cs            # World-space aim + turning the ship toward it
├── Hook/
│   └── HookLauncher.cs         # Hook flight, rope, hook head, retraction
├── Fish/
│   ├── Fish.cs                 # A single fish; delegates to its current state
│   ├── FishManager.cs          # Spawning, updating, drawing, lock-on, collection
│   ├── FishState.cs            # Base class for fish states
│   ├── FishWorldContext.cs     # What states can see of the world each frame
│   └── States/                 # IdleSwim, Panic, Resisting, Captured
├── Particles/
│   ├── Particle.cs
│   ├── ParticleSystem.cs       # Pool for spawn-then-die particles
│   ├── BubbleEmitter.cs        # Bubble trail off the submarine
│   ├── CaptureBurst.cs         # One-off burst (fish capture, hook bite)
│   └── SedimentField.cs        # Ambient drifting specks
├── Rendering/
│   ├── CameraMath.cs           # Camera banking about the camera's own forward axis
│   ├── SceneShaders.cs         # Loads and syncs the two shader variants
│   └── SubmarineModel.cs       # Loads, shades and draws the submarine model
└── Resources/
    ├── Models/Sub_DD.glb       # Submarine (+Z forward, +Y up, origin at hull center)
    └── Shaders/
        ├── fog.vs              # Vertex shader for immediate-mode shapes
        ├── model.vs            # Vertex shader for DrawModel meshes
        └── fog.fs              # Shared fragment shader: lighting + underwater fog
```

The folders are organizational only - the SDK compiles every `.cs` file regardless of where it sits.

## Roadmap / Next Steps

- 3D models for the fish (they're still colored spheres)
- Procedurally generated underwater obstacles: rocks, underwater mountains, terrain
- Further camera work
- A `Weapon` base class that harpoon, drill and others inherit from (`HookLauncher` becomes the first)
- Cursor modes matching the earlier Godot version of the game
- A throttle (the submarine currently cruises at a fixed speed)
- Fish that can break free, and line-tension mechanics
- Submarine polish: glowing headlights and a see-through windscreen (the lighting shader has no
  emission support yet)
- Sound and UI polish

## Notes

This is an early-stage prototype. Tuning values are constants near the top of the files that own them:

- `Program.cs` - world size, camera distance/height/look-ahead, hook range, lock-on angle, model scale
- `Controls/AimCursor.cs` - mouse sensitivity, turn rate and gain, how far the aim can lead the ship
- `CursorRenderer.cs` - the look of every cursor mode
- `Hook/HookLauncher.cs` - hook speed, rope look, hook size

Frame time is capped at 0.1 s so a long hitch (alt-tab, window drag) can't make the simulation take
one huge step.