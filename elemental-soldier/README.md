# Elemental Soldier

![Elemental Soldier](demo/ElementalSoldier.jpg)

A 2D side-scrolling action-platformer built with C# and [Raylib-cs](https://github.com/ChrisDill/Raylib-cs). Collect Fire, Ice, and Electric element orbs to power up your soldier and transform the world around you.

## Gameplay

Traverse a 12,000-pixel-wide world across four increasingly challenging sections. Collect element orbs with your absorb beam, then switch between them to change your bullet color and the entire environment's visual theme.

## Demo

https://github.com/user-attachments/assets/first-gameplay.mp4

## Building & Running

**Prerequisites:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
cd src
dotnet run
```

## Controls

| Key / Input        | Action                 |
| ------------------ | ---------------------- |
| WASD / Arrow Keys  | Move left / right      |
| Space / W / Up     | Jump                   |
| S / Down           | Squat (crouch)         |
| Left Mouse Click   | Shoot (aims at cursor) |
| Right Mouse Button | Absorb beam (hold)     |
| 1–4                | Toggle element slot    |
| F3                 | Debug overlay          |

## Project Structure

```
elemental-soldier/
├── src/
│   ├── Program.cs            # Entry point, game loop, camera, HUD
│   ├── Game/
│   │   ├── Player.cs         # Player movement, physics, animation, elements
│   │   ├── Bullet.cs         # Projectile logic
│   │   ├── ElementOrb.cs     # Collectible orb with beam-hit detection
│   │   ├── Background.cs     # Procedural parallax background
│   │   ├── Theme.cs          # Per-element color palettes
│   │   └── SpriteData.cs     # JSON sprite loading & runtime rendering
│   └── ElementalSoldier.csproj
├── data/sprites/
│   └── soldier.json          # Procedural pixel-art sprite data
├── dev/
│   ├── png_to_pixels.py      # PNG sprite sheet → JSON converter
│   └── preview_sprites.py    # JSON → preview image
└── *.png                     # Source sprite sheets
```

## Sprite Pipeline

Sprites are stored as JSON pixel-coordinate arrays referencing a shared color palette, not as bitmaps. To regenerate from PNG source images:

```bash
pip install Pillow
python dev/png_to_pixels.py        # generates data/sprites/soldier.json
python dev/preview_sprites.py      # optional: generates preview.png
```

## Key Features

- **Elemental Tinting** — switching elements recolors the soldier and environment with luminance-aware tinting that preserves outlines and highlights
- **Procedural Background** — runtime-generated sky, mountains, and forest layers that shift palette per element
- **Pixel-Art Rendering** — sprites drawn pixel-by-pixel from JSON data at load time, with pre-rendered flipped variants
- **Physics & Collision** — AABB collision resolution, gravity, ground detection, squat speed reduction
- **Animation State Machine** — 8 animation states with independent FPS and loop settings
- **Cross-Platform** — ships with native Raylib binaries for Windows, Linux, and macOS

## License

See repository for license details.
