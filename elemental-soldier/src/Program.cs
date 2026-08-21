using System.Numerics;
using Raylib_cs;
using ElementalSoldier.Game;

const int SCREEN_W = 1280;
const int SCREEN_H = 720;
const float GROUND_Y = 600f;

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

var player = new Player(spriteData, new Vector2(200, GROUND_Y - 64));
var background = new Background(SCREEN_W, SCREEN_H);

var platforms = new List<Platform>
{
    new Platform(0, GROUND_Y, 5000, 120, new Color(50, 60, 80)),
    new Platform(300, 480, 200, 20, new Color(70, 90, 120)),
    new Platform(600, 400, 200, 20, new Color(70, 90, 120)),
    new Platform(150, 350, 180, 20, new Color(70, 90, 120)),
    new Platform(850, 320, 250, 20, new Color(70, 90, 120)),
    new Platform(1050, 450, 180, 20, new Color(70, 90, 120)),
};

float cameraX = 0;
bool showDebug = false;
Element lastElement = Element.None;

while (!Raylib.WindowShouldClose())
{
    float dt = Raylib.GetFrameTime();

    if (Raylib.IsKeyPressed(KeyboardKey.F3))
        showDebug = !showDebug;

    if (Raylib.IsKeyPressed(KeyboardKey.One)) player.SetElement(Element.None);
    if (Raylib.IsKeyPressed(KeyboardKey.Two)) player.SetElement(Element.Fire);
    if (Raylib.IsKeyPressed(KeyboardKey.Three)) player.SetElement(Element.Ice);
    if (Raylib.IsKeyPressed(KeyboardKey.Four)) player.SetElement(Element.Electric);

    if (player.CurrentElement != lastElement)
    {
        background.Regenerate(player.CurrentElement);
        lastElement = player.CurrentElement;
    }

    player.Update(dt, platforms);

    float targetCamX = player.Position.X - SCREEN_W / 2f;
    cameraX += (targetCamX - cameraX) * 4f * dt;
    if (cameraX < 0) cameraX = 0;

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(20, 22, 30));

    // Parallax background (behind camera transform)
    background.Draw(cameraX);

    Raylib.BeginMode2D(new Camera2D(
        new Vector2(SCREEN_W / 2f, SCREEN_H / 2f),
        new Vector2(cameraX + SCREEN_W / 2f, SCREEN_H / 2f),
        0f, 1f
    ));

    foreach (var p in platforms)
        p.Draw();

    player.Draw();
    if (showDebug) player.DrawDebug();

    Raylib.EndMode2D();

    DrawHUD(player, showDebug);

    Raylib.EndDrawing();
}

Raylib.CloseWindow();

static void DrawHUD(Player player, bool showDebug)
{
    Raylib.DrawRectangle(0, 0, SCREEN_W, 50, new Color(10, 10, 15, 200));

    var elements = new[] { Element.None, Element.Fire, Element.Ice, Element.Electric };
    var labels = new[] { "Nenhum", "Fogo", "Gelo", "Eletrico" };
    var slotColors = new[]
    {
        new Color(150, 150, 150),
        new Color(255, 100, 30),
        new Color(80, 190, 255),
        new Color(255, 230, 50),
    };

    for (int i = 0; i < elements.Length; i++)
    {
        int x = 10 + i * 70;
        int y = 8;
        bool selected = player.CurrentElement == elements[i];

        Color bg = selected ? slotColors[i] : new Color(40, 40, 50);
        Raylib.DrawRectangle(x, y, 60, 34, bg);
        Raylib.DrawRectangleLines(x, y, 60, 34, selected ? Color.White : new Color(80, 80, 90));

        Raylib.DrawText($"{i + 1}", x + 4, y + 2, 14, Color.White);
        Raylib.DrawText(labels[i], x + 18, y + 10, 12, selected ? Color.White : new Color(140, 140, 150));
    }

    if (showDebug)
    {
        int y = 55;
        Raylib.DrawText($"Pos: {player.Position.X:F0}, {player.Position.Y:F0}", 10, y, 14, Color.White);
        Raylib.DrawText($"Vel: {player.Velocity.X:F0}, {player.Velocity.Y:F0}", 10, y + 18, 14, Color.White);
        Raylib.DrawText($"Anim: {player.CurrentAnimation}", 10, y + 36, 14, Color.White);
        Raylib.DrawText($"Ground: {player.OnGround}", 10, y + 54, 14, Color.White);
    }

    Raylib.DrawText("WASD/Arrows: Move | Space: Jump | 1-4: Element | F3: Debug",
        10, SCREEN_H - 25, 14, new Color(100, 100, 110));
}
