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

    public SpriteFrame Frame => Frames.Count > 0 ? Frames[CurrentFrame] : new SpriteFrame();

    public void Update(float deltaTime, float fps = 8f)
    {
        if (Frames.Count <= 1) return;
        FrameTimer += deltaTime;
        float frameDuration = 1f / fps;
        if (FrameTimer >= frameDuration)
        {
            FrameTimer -= frameDuration;
            CurrentFrame = (CurrentFrame + 1) % Frames.Count;
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

    public Texture2D RenderToTexture(string spriteName, int frameIndex, Color? tint = null)
    {
        var group = Sprites[spriteName];
        var frame = group.Frames[frameIndex];

        var image = Raylib.GenImageColor(frame.Width, frame.Height, new Color(0, 0, 0, 0));

        foreach (var px in frame.Pixels)
        {
            int x = px[0], y = px[1], colorId = px[2];
            Color c = Colors[colorId];

            if (tint.HasValue)
            {
                c = new Color(
                    (byte)(c.R * tint.Value.R / 255),
                    (byte)(c.G * tint.Value.G / 255),
                    (byte)(c.B * tint.Value.B / 255),
                    c.A
                );
            }

            Raylib.ImageDrawPixel(ref image, x, y, c);
        }

        var texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        return texture;
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
