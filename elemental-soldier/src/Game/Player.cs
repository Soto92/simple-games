using System.Numerics;
using Raylib_cs;

namespace ElementalSoldier.Game;

public enum Element
{
    None,
    Fire,
    Ice,
    Electric,
}

public class Player
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Speed = 150f;
    public float JumpForce = -350f;
    public float Gravity = 800f;
    public bool OnGround;
    public bool FacingRight = true;
    public Element CurrentElement = Element.None;
    public string CurrentAnimation = "idle";

    private SpriteData _sprites;
    private Dictionary<string, Texture2D> _textures = [];

    public Player(SpriteData sprites, Vector2 position)
    {
        _sprites = sprites;
        Position = position;
        PreRender();
    }

    private void PreRender()
    {
        foreach (var kvp in _sprites.Sprites)
            _textures[kvp.Key] = _sprites.RenderToTexture(kvp.Key, 0);
    }

    public void SetElement(Element element)
    {
        CurrentElement = element;

        Color? tint = element switch
        {
            Element.Fire => new Color(255, 120, 40, 255),
            Element.Ice => new Color(100, 200, 255, 255),
            Element.Electric => new Color(255, 255, 80, 255),
            _ => null,
        };

        foreach (var kvp in _sprites.Sprites)
        {
            if (_textures.TryGetValue(kvp.Key, out var old))
                Raylib.UnloadTexture(old);
            _textures[kvp.Key] = _sprites.RenderToTexture(kvp.Key, 0, tint);
        }
    }

    private const float MaxFallSpeed = 600f;
    private const float WorldFloor = 700f;

    public void Update(float dt, List<Platform> platforms)
    {
        float moveX = 0;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))
            moveX = -1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right))
            moveX = 1;

        if (moveX != 0) FacingRight = moveX > 0;

        Velocity.X = moveX * Speed;

        if ((Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up)) && OnGround)
        {
            Velocity.Y = JumpForce;
            OnGround = false;
        }

        Velocity.Y += Gravity * dt;
        if (Velocity.Y > MaxFallSpeed) Velocity.Y = MaxFallSpeed;

        Position.X += Velocity.X * dt;
        if (Position.X < 0) Position.X = 0;
        if (Position.X > 5000) Position.X = 5000;

        OnGround = false;
        Position.Y += Velocity.Y * dt;
        if (Position.Y > WorldFloor)
        {
            Position.Y = WorldFloor;
            Velocity.Y = 0;
            OnGround = true;
        }

        ResolveCollisions(platforms);

        UpdateAnimation();
    }

    private void ResolveCollisions(List<Platform> platforms)
    {
        OnGround = false;

        foreach (var p in platforms)
        {
            var hb = GetHitbox();
            if (!Raylib.CheckCollisionRecs(hb, p.Bounds)) continue;

            float overlapLeft = (hb.X + hb.Width) - p.Bounds.X;
            float overlapRight = (p.Bounds.X + p.Bounds.Width) - hb.X;
            float overlapTop = (hb.Y + hb.Height) - p.Bounds.Y;
            float overlapBottom = (p.Bounds.Y + p.Bounds.Height) - hb.Y;

            float minOverlap = Math.Min(overlapLeft, Math.Min(overlapRight, Math.Min(overlapTop, overlapBottom)));

            if (minOverlap == overlapTop && Velocity.Y >= 0)
            {
                Position.Y = p.Bounds.Y - hb.Height - 2;
                Velocity.Y = 0;
                OnGround = true;
            }
            else if (minOverlap == overlapBottom && Velocity.Y < 0)
            {
                Position.Y = p.Bounds.Y + p.Bounds.Height - 2;
                Velocity.Y = 0;
            }
            else if (minOverlap == overlapLeft && Velocity.X > 0)
            {
                Position.X = p.Bounds.X - hb.Width - 4;
                Velocity.X = 0;
            }
            else if (minOverlap == overlapRight && Velocity.X < 0)
            {
                Position.X = p.Bounds.X + p.Bounds.Width - 4;
                Velocity.X = 0;
            }
            else
            {
                if (overlapTop < overlapBottom)
                {
                    Position.Y = p.Bounds.Y - hb.Height - 2;
                    Velocity.Y = 0;
                    OnGround = true;
                }
                else
                {
                    Position.Y = p.Bounds.Y + p.Bounds.Height - 2;
                    Velocity.Y = 0;
                }
            }
        }
    }

    private void UpdateAnimation()
    {
        if (!OnGround)
            CurrentAnimation = Velocity.Y < 0 ? "jump" : "fall";
        else if (MathF.Abs(Velocity.X) > 10)
            CurrentAnimation = "run";
        else
            CurrentAnimation = "idle";
    }

    public void Draw()
    {
        if (!_textures.TryGetValue(CurrentAnimation, out var tex))
            return;

        var frame = _sprites.Sprites[CurrentAnimation].Frame;

        if (FacingRight)
        {
            Raylib.DrawTextureEx(tex, new Vector2(Position.X, Position.Y), 0, 1f, Color.White);
        }
        else
        {
            var src = new Rectangle(0, 0, frame.Width, frame.Height);
            var dst = new Rectangle(Position.X + frame.Width, Position.Y, -frame.Width, frame.Height);
            var origin = new Vector2(frame.Width, 0);
            Raylib.DrawTexturePro(tex, src, dst, origin, 0f, Color.White);
        }
    }

    public Rectangle GetHitbox()
    {
        return new Rectangle(Position.X + 4, Position.Y + 2, 16, 62);
    }

    public void DrawDebug()
    {
        var hb = GetHitbox();
        Raylib.DrawRectangleLines((int)hb.X, (int)hb.Y, (int)hb.Width, (int)hb.Height, Color.Green);
    }
}

public class Platform
{
    public Rectangle Bounds;
    public Color DebugColor;

    public Platform(float x, float y, float w, float h, Color? color = null)
    {
        Bounds = new Rectangle(x, y, w, h);
        DebugColor = color ?? new Color(80, 80, 100, 255);
    }

    public void Draw()
    {
        Raylib.DrawRectangleRec(Bounds, DebugColor);
        Raylib.DrawRectangleLines((int)Bounds.X, (int)Bounds.Y, (int)Bounds.Width, (int)Bounds.Height, Color.White);
    }
}
