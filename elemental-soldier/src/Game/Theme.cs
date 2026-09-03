using Raylib_cs;

namespace ElementalSoldier.Game;

/// <summary>
/// Game color theme. Change values here to recolor everything.
/// </summary>
public static class Theme
{
    // ---------------------------------------------------------------
    // Environment / Background
    // ---------------------------------------------------------------
    public static readonly Color Sky = new(20, 22, 30);
    public static readonly Color Boundary = new(255, 80, 80, 120);

    // Default sky (None element)
    public static readonly Color SkyTop = new(15, 18, 40);
    public static readonly Color SkyBottom = new(40, 50, 80);

    // Sky stars
    public static readonly Color SunSky = new(255, 200, 100, 200);
    public static readonly Color StarSky = new(200, 210, 240, 180);

    // Mountains (far = more distant/darker, near = closer/lighter)
    public static readonly Color MountainFar = new(25, 30, 50);
    public static readonly Color MountainNear = new(35, 42, 65);

    // Forest (two tree layers)
    public static readonly Color ForestFar = new(20, 35, 30);
    public static readonly Color ForestNear = new(15, 28, 22);

    // ---------------------------------------------------------------
    // Platforms
    // ---------------------------------------------------------------
    public static readonly Color PlatformGround = new(50, 60, 80);
    public static readonly Color PlatformFill = new(70, 90, 120);
    public static readonly Color PlatformShine = new(90, 105, 130);
    public static readonly Color PlatformDebug = new(80, 80, 100, 255);

    // ---------------------------------------------------------------
    // HUD
    // ---------------------------------------------------------------
    public static readonly Color HudBar = new(10, 10, 15, 200);
    public static readonly Color HudSlotBg = new(40, 40, 50);
    public static readonly Color HudSlotBorder = new(80, 80, 90);
    public static readonly Color HudText = new(140, 140, 150);
    public static readonly Color HudHint = new(100, 100, 110);

    // Progress bar
    public static readonly Color ProgressBg = HudSlotBg;
    public static readonly Color ProgressFill = new(80, 190, 255);

    // ---------------------------------------------------------------
    // Elements (HUD slots)
    // ---------------------------------------------------------------
    public static readonly Color ElementNeutral = new(150, 150, 150);
    public static readonly Color ElementFire = new(255, 100, 30);
    public static readonly Color ElementIce = new(80, 190, 255);
    public static readonly Color ElementElectric = new(255, 230, 50);

    // Tint applied to the soldier sprite per element
    public static readonly Color TintFire = new(255, 120, 40, 255);
    public static readonly Color TintIce = new(100, 200, 255, 255);
    public static readonly Color TintElectric = new(255, 255, 80, 255);

    // Bullet color per element
    public static readonly Color BulletNeutral = Color.White;
    public static readonly Color BulletFire = new(255, 130, 40, 255);
    public static readonly Color BulletIce = new(100, 210, 255, 255);
    public static readonly Color BulletElectric = new(255, 240, 80, 255);

    // ---------------------------------------------------------------
    // Background palettes per element
    // ---------------------------------------------------------------
    public static (Color Top, Color Bottom) SkyPalette(Element element) => element switch
    {
        Element.Fire => (new(30, 10, 10), new(80, 30, 15)),
        Element.Ice => (new(10, 18, 35), new(25, 50, 80)),
        Element.Electric => (new(15, 15, 30), new(40, 40, 70)),
        _ => (SkyTop, SkyBottom),
    };

    public static (Color Dark, Color Light) MountainPalette(Element element) => element switch
    {
        Element.Fire => (new(40, 15, 10), new(65, 25, 15)),
        Element.Ice => (new(15, 25, 45), new(30, 45, 70)),
        Element.Electric => (new(25, 25, 45), new(40, 40, 65)),
        _ => (MountainFar, MountainNear),
    };

    public static (Color Dark, Color Light) ForestPalette(Element element) => element switch
    {
        Element.Fire => (new(35, 12, 8), new(55, 20, 12)),
        Element.Ice => (new(12, 22, 38), new(22, 38, 55)),
        Element.Electric => (new(20, 20, 38), new(32, 32, 52)),
        _ => (ForestFar, ForestNear),
    };
}