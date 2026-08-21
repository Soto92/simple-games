using System.Numerics;
using Raylib_cs;

namespace ElementalSoldier.Game;

public class Background : IDisposable
{
    private Texture2D _skyTexture;
    private Texture2D _mountainTexture;
    private Texture2D _forestTexture;

    private int _screenW;
    private int _screenH;

    private static readonly Color SkyTopDefault = new(15, 18, 40);
    private static readonly Color SkyBotDefault = new(40, 50, 80);
    private static readonly Color MountainFar = new(25, 30, 50);
    private static readonly Color MountainNear = new(35, 42, 65);
    private static readonly Color ForestFar = new(20, 35, 30);
    private static readonly Color ForestNear = new(15, 28, 22);

    public Background(int screenW, int screenH)
    {
        _screenW = screenW;
        _screenH = screenH;
        Regenerate(Element.None);
    }

    public void Regenerate(Element element)
    {
        FreeTexture(ref _skyTexture);
        FreeTexture(ref _mountainTexture);
        FreeTexture(ref _forestTexture);

        _skyTexture = GenerateSky(element);
        _mountainTexture = GenerateMountains(element);
        _forestTexture = GenerateForest(element);
    }

    public void Draw(float cameraX)
    {
        float skyOffset = -cameraX * 0.02f;
        float mountainOffset = -cameraX * 0.15f;
        float forestOffset = -cameraX * 0.4f;

        Raylib.DrawTextureEx(_skyTexture, new Vector2(skyOffset, 0), 0, 1f, Color.White);
        Raylib.DrawTextureEx(_mountainTexture, new Vector2(mountainOffset, 0), 0, 1f, Color.White);
        Raylib.DrawTextureEx(_forestTexture, new Vector2(forestOffset, 0), 0, 1f, Color.White);
    }

    public void Dispose()
    {
        FreeTexture(ref _skyTexture);
        FreeTexture(ref _mountainTexture);
        FreeTexture(ref _forestTexture);
    }

    private static void FreeTexture(ref Texture2D tex)
    {
        if (tex.Id != 0)
        {
            Raylib.UnloadTexture(tex);
            tex = default;
        }
    }

    private Texture2D GenerateSky(Element element)
    {
        var (top, bot) = element switch
        {
            Element.Fire => (new Color(30, 10, 10), new Color(80, 30, 15)),
            Element.Ice => (new Color(10, 18, 35), new Color(25, 50, 80)),
            Element.Electric => (new Color(15, 15, 30), new Color(40, 40, 70)),
            _ => (SkyTopDefault, SkyBotDefault),
        };

        var image = Raylib.GenImageColor(_screenW, _screenH, top);

        for (int y = 0; y < _screenH; y++)
        {
            float t = (float)y / _screenH;
            byte r = (byte)(top.R + (bot.R - top.R) * t);
            byte g = (byte)(top.G + (bot.G - top.G) * t);
            byte b = (byte)(top.B + (bot.B - top.B) * t);
            Raylib.ImageDrawRectangle(ref image, 0, y, _screenW, 1, new Color(r, g, b, (byte)255));
        }

        if (element != Element.Ice)
        {
            var starColor = element == Element.Fire
                ? new Color((byte)255, (byte)200, (byte)100, (byte)200)
                : new Color((byte)200, (byte)210, (byte)240, (byte)180);

            var rng = new Random(42);
            for (int i = 0; i < 60; i++)
            {
                int sx = rng.Next(_screenW);
                int sy = rng.Next(_screenH / 2);
                byte alpha = (byte)(80 + rng.Next(120));
                Raylib.ImageDrawPixel(ref image, sx, sy, new Color(starColor.R, starColor.G, starColor.B, alpha));
            }
        }

        var tex = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        return tex;
    }

    private Texture2D GenerateMountains(Element element)
    {
        var (dark, light) = element switch
        {
            Element.Fire => (new Color((byte)40, (byte)15, (byte)10), new Color((byte)65, (byte)25, (byte)15)),
            Element.Ice => (new Color((byte)15, (byte)25, (byte)45), new Color((byte)30, (byte)45, (byte)70)),
            Element.Electric => (new Color((byte)25, (byte)25, (byte)45), new Color((byte)40, (byte)40, (byte)65)),
            _ => (MountainFar, MountainNear),
        };

        int texH = _screenH;
        var image = Raylib.GenImageColor(_screenW, texH, new Color((byte)0, (byte)0, (byte)0, (byte)0));

        DrawRidge(ref image, texH, dark, 0.008f, 0.35f, 0.65f, 100);
        DrawRidge(ref image, texH, light, 0.012f, 0.45f, 0.75f, 200);

        var tex = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        return tex;
    }

    private void DrawRidge(ref Image image, int texH, Color color, float freq, float minY, float maxY, int seed)
    {
        int w = image.Width;
        int baseY = (int)(texH * maxY);
        int topY = (int)(texH * minY);

        for (int x = 0; x < w; x++)
        {
            float nx = x * freq + seed;
            float n = SmoothNoise(nx);
            float mountainY = topY + (baseY - topY) * (1f - n);

            for (int y = (int)mountainY; y < texH; y++)
            {
                float depthFade = Math.Clamp((y - mountainY) / 30f, 0, 1);
                float mult = 0.7f + 0.3f * depthFade;
                byte r = (byte)(color.R * mult);
                byte g = (byte)(color.G * mult);
                byte b = (byte)(color.B * mult);
                Raylib.ImageDrawPixel(ref image, x, y, new Color(r, g, b, (byte)255));
            }
        }
    }

    private Texture2D GenerateForest(Element element)
    {
        var (dark, light) = element switch
        {
            Element.Fire => (new Color((byte)35, (byte)12, (byte)8), new Color((byte)55, (byte)20, (byte)12)),
            Element.Ice => (new Color((byte)12, (byte)22, (byte)38), new Color((byte)22, (byte)38, (byte)55)),
            Element.Electric => (new Color((byte)20, (byte)20, (byte)38), new Color((byte)32, (byte)32, (byte)52)),
            _ => (ForestFar, ForestNear),
        };

        int texH = _screenH;
        var image = Raylib.GenImageColor(_screenW, texH, new Color((byte)0, (byte)0, (byte)0, (byte)0));
        var rng = new Random(777);

        DrawTreeLine(ref image, texH, dark, 0.65f, 0.85f, rng, 3, 10);
        DrawTreeLine(ref image, texH, light, 0.55f, 0.95f, rng, 5, 15);

        var tex = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        return tex;
    }

    private void DrawTreeLine(ref Image image, int texH, Color baseColor,
        float minY, float maxY, Random rng, int minH, int maxH)
    {
        int w = image.Width;
        float groundY = texH * maxY;

        int x = rng.Next(5, 20);
        while (x < w)
        {
            int treeH = rng.Next(minH, maxH + 1);
            int treeW = rng.Next(2, Math.Max(3, treeH / 3 + 1));
            int trunkH = Math.Max(1, treeH / 4);
            float baseY = groundY - rng.Next(0, 15);

            for (int ty = 0; ty < trunkH; ty++)
            {
                int py = (int)(baseY - ty);
                if (py >= 0 && py < texH && x >= 0 && x < w)
                    Raylib.ImageDrawPixel(ref image, x, py, new Color(
                        (byte)(baseColor.R * 0.8f),
                        (byte)(baseColor.G * 0.8f),
                        (byte)(baseColor.B * 0.8f), (byte)255));
            }

            for (int row = 0; row < treeH - trunkH; row++)
            {
                float progress = (float)row / (treeH - trunkH);
                int halfW = (int)(treeW * (1f - progress * 0.8f)) + 1;
                int cy = (int)(baseY - trunkH - row);
                if (cy < 0 || cy >= texH) continue;

                for (int dx = -halfW; dx <= halfW; dx++)
                {
                    int cx = x + dx;
                    if (cx < 0 || cx >= w) continue;

                    float edgeFade = 1f - MathF.Abs(dx) / (float)halfW;
                    byte fade = (byte)(180 + 75 * edgeFade);
                    byte r = (byte)Math.Min(255, baseColor.R * fade / 255);
                    byte g = (byte)Math.Min(255, baseColor.G * fade / 255);
                    byte b = (byte)Math.Min(255, baseColor.B * fade / 255);

                    Raylib.ImageDrawPixel(ref image, cx, cy, new Color(r, g, b, (byte)255));
                }
            }

            x += treeW + rng.Next(4, 14);
        }
    }

    private static float Hash(float x)
    {
        int ix = (int)MathF.Floor(x);
        uint n = (uint)(ix * 1597334673);
        n ^= n >> 16;
        n *= 0x85ebca6b;
        n ^= n >> 13;
        n *= 0xc2b2ae35;
        n ^= n >> 16;
        return (n & 0xFFFF) / (float)0xFFFF;
    }

    private static float SmoothNoise(float x)
    {
        float i = MathF.Floor(x);
        float f = x - i;
        float u = f * f * (3f - 2f * f);
        return Hash(i) * (1f - u) + Hash(i + 1f) * u;
    }
}
