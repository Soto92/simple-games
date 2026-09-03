using System.Numerics;
using Raylib_cs;
using ElementalSoldier.Game;

const int SCREEN_W = 1280;
const int SCREEN_H = 720;
const float GROUND_Y = 600f;
const float WORLD_WIDTH = 12000f;
const float MAX_CAMERA_X = WORLD_WIDTH - SCREEN_W;

Raylib.InitWindow(SCREEN_W, SCREEN_H, "Elemental Soldier");
Raylib.SetTargetFPS(60);

string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sprites", "soldier.json");
if (!File.Exists(dataPath))
    dataPath = Path.Combine("data", "sprites", "soldier.json");

Console.WriteLine("Loading: " + dataPath);
var spriteData = SpriteData.Load(dataPath);

Console.WriteLine("Sprites:");
foreach (var kvp in spriteData.Sprites)
    Console.WriteLine($"  {kvp.Key}: {kvp.Value.Frames.Count} frames");

var player = new Player(spriteData, new Vector2(200, GROUND_Y - 64))
{
    WorldWidth = WORLD_WIDTH,
};
var background = new Background(SCREEN_W, SCREEN_H);

var platforms = LevelGenerator.CreatePlatforms(WORLD_WIDTH, GROUND_Y);

// Test orbs: absorb them (or touch them) to gain that element
var orbs = new List<ElementOrb>
{
    new(new Vector2(700, GROUND_Y - 30), Element.Fire),
    new(new Vector2(1050, GROUND_Y - 30), Element.Ice),
    new(new Vector2(1400, GROUND_Y - 30), Element.Electric),
};

float cameraX = 0;
bool showDebug = false;
var bullets = new List<Bullet>();

while (!Raylib.WindowShouldClose())
{
    float dt = Raylib.GetFrameTime();

    if (Raylib.IsKeyPressed(KeyboardKey.F3))
        showDebug = !showDebug;

    if (Raylib.IsKeyPressed(KeyboardKey.One) && player.Acquired.Count > 0) ToggleSlot(player, player.Acquired[0]);
    if (Raylib.IsKeyPressed(KeyboardKey.Two) && player.Acquired.Count > 1) ToggleSlot(player, player.Acquired[1]);
    if (Raylib.IsKeyPressed(KeyboardKey.Three) && player.Acquired.Count > 2) ToggleSlot(player, player.Acquired[2]);
    if (Raylib.IsKeyPressed(KeyboardKey.Four) && player.Acquired.Count > 3) ToggleSlot(player, player.Acquired[3]);

    float targetCamX = player.Position.X - SCREEN_W / 2f;
    cameraX += (targetCamX - cameraX) * 4f * dt;
    cameraX = Math.Clamp(cameraX, 0, MAX_CAMERA_X);

    var camera2d = new Camera2D(
        new Vector2(SCREEN_W / 2f, SCREEN_H / 2f),
        new Vector2(cameraX + SCREEN_W / 2f, SCREEN_H / 2f),
        0f, 1f
    );

    // Shoot: mouse click aims at the cursor position
    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        player.StartAttack(Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera2d));

    // Absorb: hold right mouse button to suck energy from a target point
    if (Raylib.IsMouseButtonDown(MouseButton.Right))
        player.StartAbsorb(Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera2d));
    else
        player.EndAbsorb();

    player.Update(dt, platforms);

    // Orb pickup: absorb beam or direct touch grants the element
    foreach (var orb in orbs)
    {
        if (orb.Collected) continue;
        orb.Update(dt);

        if ((player.IsAbsorbing && orb.BeamHits(player.GetMuzzlePosition(), player.AbsorbTarget))
            || Raylib.CheckCollisionCircleRec(orb.Position, orb.Radius, player.GetHitbox()))
        {
            orb.Collected = true;
            player.Acquire(orb.Element);
        }
    }

    // Actual shot fired after the attack freeze
    if (player.HasPendingShot)
    {
        var muzzle = player.GetMuzzlePosition();
        Vector2 dir = player.ShootTarget - muzzle;
        if (dir.LengthSquared() < 0.01f)
            dir = new Vector2(player.FacingRight ? 1f : -1f, 0f);
        dir = Vector2.Normalize(dir);

        bullets.Add(new Bullet(muzzle, dir * 900f, ElementBulletColor(player.CurrentElement)));
        player.HasPendingShot = false;
    }

    foreach (var b in bullets)
        b.Update(dt, platforms);
    bullets.RemoveAll(b => b.Dead);

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Theme.Sky);

    background.Draw(cameraX);

    Raylib.BeginMode2D(camera2d);

    foreach (var p in platforms)
        p.Draw();

    foreach (var b in bullets)
        b.Draw();

    foreach (var orb in orbs.Where(o => !o.Collected))
        orb.Draw();

    // World boundary markers
    Raylib.DrawRectangleLines(0, 0, 4, SCREEN_H, Theme.Boundary);
    Raylib.DrawRectangleLines((int)WORLD_WIDTH, 0, 4, SCREEN_H, Theme.Boundary);

    player.Draw();

    // Absorb beam: energy line from the soldier to the absorb target
    if (player.IsAbsorbing)
    {
        var beamStart = player.GetMuzzlePosition();
        var beamEnd = player.AbsorbTarget;
        Color beamColor = ElementBulletColor(player.CurrentElement);

        Raylib.DrawLineEx(beamStart, beamEnd, 9f, new Color(beamColor.R, beamColor.G, beamColor.B, (byte)60));
        Raylib.DrawLineEx(beamStart, beamEnd, 3f, beamColor);
        Raylib.DrawLineEx(beamStart, beamEnd, 1f, Color.White);
    }

    if (showDebug) player.DrawDebug();

    Raylib.EndMode2D();

    DrawHUD(player, showDebug, cameraX, MAX_CAMERA_X);

    Raylib.EndDrawing();
}

Raylib.CloseWindow();

static void DrawHUD(Player player, bool showDebug, float cameraX, float maxCameraX)
{
Raylib.DrawRectangle(0, 0, SCREEN_W, 50, Theme.HudBar);

    for (int i = 0; i < 4; i++)
    {
        int x = 10 + i * 70;
        int y = 8;
        bool filled = i < player.Acquired.Count;
        Element e = filled ? player.Acquired[i] : Element.None;
        bool selected = filled && player.CurrentElement == e;

        Color bg = selected ? ElementSlotColor(e) : Theme.HudSlotBg;
        Raylib.DrawRectangle(x, y, 60, 34, bg);
        Raylib.DrawRectangleLines(x, y, 60, 34, selected ? Color.White : Theme.HudSlotBorder);

        Raylib.DrawText($"{i + 1}", x + 4, y + 2, 14, Color.White);

        // Element ball appears in the first available slot once collected
        if (filled)
            DrawMiniOrb(new Vector2(x + 30, y + 18), ElementSlotColor(e));
    }

    // Progress bar (world position)
    float progress = Math.Clamp(player.Position.X / maxCameraX, 0, 1);
    Raylib.DrawRectangle(SCREEN_W - 220, 15, 200, 12, Theme.ProgressBg);
    Raylib.DrawRectangle(SCREEN_W - 220, 15, (int)(200 * progress), 12, Theme.ProgressFill);
    Raylib.DrawText($"{player.Position.X:F0}", SCREEN_W - 220, 29, 12, Theme.HudText);

    if (showDebug)
    {
        int y = 55;
        Raylib.DrawText($"Pos: {player.Position.X:F0}, {player.Position.Y:F0}", 10, y, 14, Color.White);
        Raylib.DrawText($"Vel: {player.Velocity.X:F0}, {player.Velocity.Y:F0}", 10, y + 18, 14, Color.White);
        Raylib.DrawText($"Cam: {cameraX:F0}/{maxCameraX:F0}", 10, y + 36, 14, Color.White);
        Raylib.DrawText($"Anim: {player.CurrentAnimation}", 10, y + 54, 14, Color.White);
        Raylib.DrawText($"Ground: {player.OnGround}", 10, y + 72, 14, Color.White);
    }

    Raylib.DrawText("WASD/Arrows: Move | Space: Jump | Down/S: Squat | Click: Shoot | Right-Click: Absorb | 1-4: Element | F3: Debug",
        10, SCREEN_H - 25, 14, Theme.HudHint);
}

static Color ElementBulletColor(Element element)
{
    return element switch
    {
        Element.Fire => Theme.BulletFire,
        Element.Ice => Theme.BulletIce,
        Element.Electric => Theme.BulletElectric,
        _ => Theme.BulletNeutral,
    };
}

static Color ElementSlotColor(Element element)
{
    return element switch
    {
        Element.Fire => Theme.ElementFire,
        Element.Ice => Theme.ElementIce,
        Element.Electric => Theme.ElementElectric,
        _ => Theme.ElementNeutral,
    };
}

static void ToggleSlot(Player player, Element element)
{
    if (player.CurrentElement == element)
        player.SetElement(Element.None);
    else
        player.SetElement(element);
}

static void DrawMiniOrb(Vector2 center, Color color)
{
    Raylib.DrawCircleV(center, 9f, new Color(color.R, color.G, color.B, (byte)60));
    Raylib.DrawCircleV(center, 7f, color);
    Raylib.DrawCircleV(center + new Vector2(-2, -2), 2.5f, Color.White);
}

static class LevelGenerator
{
    public static List<Platform> CreatePlatforms(float worldWidth, float groundY)
    {
        var platforms = new List<Platform>
        {
            // Ground - whole world
            new Platform(0, groundY, worldWidth, 120, Theme.PlatformGround),
        };

        // Sun rays on the ground (decoration)
        var rng = new Random(1337);
        float x = 50;
        while (x < worldWidth - 40)
        {
            float w = rng.Next(4, 10);
            platforms.Add(new Platform(x, groundY + 6, w, 3, Theme.PlatformShine));
            x += rng.Next(30, 90);
        }

        // Section 1 - Tutorial (0-2000)
        platforms.Add(new Platform(300, 480, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(600, 400, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(150, 350, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(850, 320, 250, 20, Theme.PlatformFill));
        platforms.Add(new Platform(1050, 450, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(1300, 380, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(1550, 300, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(1750, 220, 240, 20, Theme.PlatformFill));

        // Section 2 - Floating islands (2000-5000)
        platforms.Add(new Platform(2200, 480, 150, 20, Theme.PlatformFill));
        platforms.Add(new Platform(2450, 400, 150, 20, Theme.PlatformFill));
        platforms.Add(new Platform(2700, 330, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(3000, 400, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(3350, 320, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(3600, 250, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(3900, 400, 260, 20, Theme.PlatformFill));
        platforms.Add(new Platform(4250, 330, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(4500, 260, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(4700, 450, 200, 20, Theme.PlatformFill));

        // Section 3 - Towers and heights (5000-8000)
        platforms.Add(new Platform(5200, 450, 150, 20, Theme.PlatformFill));
        platforms.Add(new Platform(5450, 370, 150, 20, Theme.PlatformFill));
        platforms.Add(new Platform(5700, 290, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(6000, 400, 260, 20, Theme.PlatformFill));
        platforms.Add(new Platform(6350, 320, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(6650, 240, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(6900, 150, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(7100, 340, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(7400, 260, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(7650, 180, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(7900, 420, 200, 20, Theme.PlatformFill));

        // Section 4 - Final (8000-12000)
        platforms.Add(new Platform(8250, 350, 180, 20, Theme.PlatformFill));
        platforms.Add(new Platform(8550, 280, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(8850, 200, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(9200, 350, 260, 20, Theme.PlatformFill));
        platforms.Add(new Platform(9550, 270, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(9850, 190, 240, 20, Theme.PlatformFill));
        platforms.Add(new Platform(10200, 350, 280, 20, Theme.PlatformFill));
        platforms.Add(new Platform(10600, 260, 200, 20, Theme.PlatformFill));
        platforms.Add(new Platform(10900, 180, 250, 20, Theme.PlatformFill));
        platforms.Add(new Platform(11300, 300, 220, 20, Theme.PlatformFill));
        platforms.Add(new Platform(11600, 200, 380, 20, Theme.PlatformFill));

        return platforms;
    }
}