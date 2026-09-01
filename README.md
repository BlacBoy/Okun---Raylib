# Okun: Submarine Odyssey

A 3D submarine exploration game built with C# and [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

This repository currently contains a **test render** — a stand-in cube submarine, an
ocean-floor grid, and a working third-person camera/control rig — used to validate the
core movement and camera feel before real models and mechanics are built.

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or later)
- [Raylib-cs](https://www.nuget.org/packages/Raylib-cs) NuGet package

## Getting Started

```bash
git clone <your-repo-url>
cd Okun
dotnet restore
dotnet run
```

## Controls

| Input          | Action                        |
|----------------|--------------------------------|
| Mouse          | Look around (camera orbit)     |
| `W`            | Move submarine up              |
| `S`            | Move submarine down            |
| `A`            | Roll submarine left            |
| `D`            | Roll submarine right           |
| `Esc`          | Close the window               |

The mouse is captured on launch (locked and hidden) so camera look-around isn't limited
by the edges of the screen.

## Camera

The camera runs in a third-person orbit around the submarine, positioned just above and
behind it. Mouse movement adjusts the orbit's yaw and pitch; the camera always looks at
the submarine's current position.

## Project Structure

```
Okun/
├── Program.cs      # Entry point: window setup, camera, controls, render loop
└── README.md
```

## Roadmap / Next Steps

- Replace the placeholder cube with a real submarine model
- Add forward/backward thrust and turning
- Ocean environment art (terrain, lighting, fog/visibility falloff)
- Sound and UI polish

## Notes

This is an early-stage prototype. Camera and control values (sensitivity, distance,
speed) are set as constants near the top of `Main()` in `Program.cs` for quick tuning
during playtesting.
