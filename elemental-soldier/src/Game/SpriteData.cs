using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;

namespace ElementalSoldier.Game;

public class SpriteFrame
{
    [JsonPropertyName("w")]
    public int Width { get; set; }

    [JsonPropertyName("h")]
    public int Height { get; set; }

    [JsonPropertyName("px")]
    public int[][] Pixels { get; set; } = [];
}

public class SpriteGroup
{
    public List<SpriteFrame> Frames { get; set; } = [];
    public int CurrentFrame { get; set; }
    public float FrameTimer { get; set; }
    public bool Loop { get; set; } = true;

    public SpriteFrame Frame => Frames.Count > 0 ? Frames[CurrentFrame] : new SpriteFrame();

    public void Update(float deltaTime, float fps = 8f)
    {
        if (Frames.Count <= 1) return;
        FrameTimer += deltaTime;
        float frameDuration = 1f / fps;
        if (FrameTimer >= frameDuration)
        {
            FrameTimer -= frameDuration;
            CurrentFrame++;
            if (CurrentFrame >= Frames.Count)
                CurrentFrame = Loop ? 0 : Frames.Count - 1;
        }
    }

    public void Reset()
    {
        CurrentFrame = 0;
        FrameTimer = 0;
    }
}

public class SpriteData
{
    public Color[] Colors { get; set; } = [];
    public Dictionary<string, SpriteGroup> Sprites { get; set; } = [];

    public static SpriteData Load(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);
        var raw = JsonSerializer.Deserialize<RawSpriteData>(json);
        if (raw == null) throw new Exception("Failed to load sprite data");

        var data = new SpriteData();

        data.Colors = new Color[raw.Colors.Length];
        for (int i = 0; i < raw.Colors.Length; i++)
        {
            if (raw.Colors[i] == null)
            {
                data.Colors[i] = new Color(0, 0, 0, 0);
                continue;
            }
            var c = raw.Colors[i]!;
            data.Colors[i] = new Color((byte)c[0], (byte)c[1], (byte)c[2], (byte)c[3]);
        }

        foreach (var kvp in raw.Sprites)
        {
            var group = new SpriteGroup();
            foreach (var rawFrame in kvp.Value)
            {
                var frame = new SpriteFrame
                {
                    Width = rawFrame.W,
                    Height = rawFrame.H,
                    Pixels = rawFrame.Px,
                };
                group.Frames.Add(frame);
            }
            data.Sprites[kvp.Key] = group;
        }

        return data;
    }

    public Texture2D RenderToTexture(string spriteName, int frameIndex, Color? tint = null, bool flipX = false)
    {
        var group = Sprites[spriteName];
        var frame = group.Frames[frameIndex];

        var image = Raylib.GenImageColor(frame.Width, frame.Height, new Color(0, 0, 0, 0));

        foreach (var px in frame.Pixels)
        {
            int x = px[0], y = px[1], colorId = px[2];
            if (flipX) x = frame.Width - 1 - x;

            Color c = Colors[colorId];

            if (tint.HasValue)
            {
                // Partial tint: only mid-tone armor pixels get the element color.
                // Dark shadows/outlines and bright highlights keep their original color,
                // so the sprite stays readable instead of becoming a flat silhouette.
                float lum = (0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B) / 255f;
                float mix = SmoothStep(0.25f, 0.55f, lum) * (1f - SmoothStep(0.72f, 0.92f, lum));
                float shaded = 0.45f + 0.55f * lum;

                c = new Color(
                    (byte)(c.R + ((byte)(tint.Value.R * shaded) - c.R) * mix),
                    (byte)(c.G + ((byte)(tint.Value.G * shaded) - c.G) * mix),
                    (byte)(c.B + ((byte)(tint.Value.B * shaded) - c.B) * mix),
                    c.A
                );
            }

            Raylib.ImageDrawPixel(ref image, x, y, c);
        }

        var texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        return texture;
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}

public class RawSpriteData
{
    [JsonPropertyName("colors")]
    public int?[][] Colors { get; set; } = [];

    [JsonPropertyName("sprites")]
    public Dictionary<string, List<RawFrame>> Sprites { get; set; } = [];
}

public class RawFrame
{
    [JsonPropertyName("w")]
    public int W { get; set; }

    [JsonPropertyName("h")]
    public int H { get; set; }

    [JsonPropertyName("px")]
    public int[][] Px { get; set; } = [];
}
