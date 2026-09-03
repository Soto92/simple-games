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
    public float Speed = 230f;
    public float JumpForce = -350f;
    public float Gravity = 800f;
    public bool OnGround;
    public bool FacingRight = true;
    public Element CurrentElement = Element.None;
    public List<Element> Acquired = new();
    public string CurrentAnimation = "idle";
    public bool IsSquatting;
    public bool IsAttacking;
    public bool HasPendingShot;
    public Vector2 ShootTarget;
    public bool IsAbsorbing;
    public Vector2 AbsorbTarget;
    private float _attackFreeze;
    private const float AttackHoldTime = 0f;

    private SpriteData _sprites;
    private Dictionary<string, List<Texture2D>> _textures = [];
    private Dictionary<string, List<Texture2D>> _texturesFlipped = [];

    // Animation FPS per state
    private static readonly Dictionary<string, float> AnimFps = new()
    {
        ["idle"] = 6f,
        ["run"] = 12f,
        ["jump"] = 6f,
        ["attack"] = 12f,
        ["dead"] = 8f,
        ["absorb"] = 10f,
        ["hit"] = 12f,
        ["squat"] = 10f,
    };

    public Player(SpriteData sprites, Vector2 position)
    {
        _sprites = sprites;
        Position = position;
        PreRender();
    }

    private void PreRender()
    {
        foreach (var kvp in _sprites.Sprites)
        {
            var frames = new List<Texture2D>();
            var framesFlipped = new List<Texture2D>();
            for (int i = 0; i < kvp.Value.Frames.Count; i++)
            {
                frames.Add(_sprites.RenderToTexture(kvp.Key, i));
                framesFlipped.Add(_sprites.RenderToTexture(kvp.Key, i, null, flipX: true));
            }
            _textures[kvp.Key] = frames;
            _texturesFlipped[kvp.Key] = framesFlipped;
        }
    }

    public void SetElement(Element element)
    {
        CurrentElement = element;

        Color? tint = element switch
        {
            Element.Fire => Theme.TintFire,
            Element.Ice => Theme.TintIce,
            Element.Electric => Theme.TintElectric,
            _ => null,
        };

        foreach (var kvp in _sprites.Sprites)
        {
            FreeTextures(_textures[kvp.Key]);
            FreeTextures(_texturesFlipped[kvp.Key]);

            _textures[kvp.Key] = [];
            _texturesFlipped[kvp.Key] = [];
            for (int i = 0; i < kvp.Value.Frames.Count; i++)
            {
                _textures[kvp.Key].Add(_sprites.RenderToTexture(kvp.Key, i, tint));
                _texturesFlipped[kvp.Key].Add(_sprites.RenderToTexture(kvp.Key, i, tint, flipX: true));
            }
        }
    }

    public void Acquire(Element element)
    {
        if (element != Element.None && !Acquired.Contains(element))
            Acquired.Add(element);
        SetElement(element);
    }

    private static void FreeTextures(List<Texture2D> list)
    {
        foreach (var tex in list)
            Raylib.UnloadTexture(tex);
    }

    public void StartAttack(Vector2 targetWorld)
    {
        if (IsAttacking || IsAbsorbing) return;
        IsAttacking = true;
        ShootTarget = targetWorld;
        _attackFreeze = AttackHoldTime;
    }

    public void StartAbsorb(Vector2 targetWorld)
    {
        AbsorbTarget = targetWorld;
        IsAbsorbing = true;
        IsAttacking = false; // cancel pending attack
    }

    public void EndAbsorb()
    {
        IsAbsorbing = false;
    }

    public Vector2 GetMuzzlePosition()
    {
        float dir = FacingRight ? 1f : -1f;
        return new Vector2(Position.X + dir * 24f, Position.Y + 30f);
    }

    private const float MaxFallSpeed = 600f;
    private const float WorldFloor = 700f;
    public float WorldWidth = 12000f;

    public void Update(float dt, List<Platform> platforms)
    {
        IsSquatting = OnGround && (
            Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down));

        float moveX = 0;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))
            moveX = -1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right))
            moveX = 1;

        if (moveX != 0) FacingRight = moveX > 0;

        if (IsAbsorbing) moveX = 0;

        float speed = IsSquatting ? Speed * 0.35f : Speed;
        Velocity.X = moveX * speed;

        if ((Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up)) && OnGround && !IsSquatting && !IsAbsorbing)
        {
            Velocity.Y = JumpForce;
            OnGround = false;
        }

        Velocity.Y += Gravity * dt;
        if (Velocity.Y > MaxFallSpeed) Velocity.Y = MaxFallSpeed;

        Position.X += Velocity.X * dt;
        if (Position.X < 0) Position.X = 0;
        if (Position.X > WorldWidth) Position.X = WorldWidth;

        OnGround = false;
        Position.Y += Velocity.Y * dt;
        if (Position.Y > WorldFloor)
        {
            Position.Y = WorldFloor;
            Velocity.Y = 0;
            OnGround = true;
        }

        ResolveCollisions(platforms);

        UpdateAnimation(dt);
    }

    private void UpdateAnimation(float dt)
    {
        string newAnim;
        if (IsAbsorbing)
            newAnim = "absorb";
        else if (IsAttacking)
            newAnim = "attack";
        else if (!OnGround)
            newAnim = "jump";
        else if (IsSquatting)
            newAnim = "squat";
        else if (MathF.Abs(Velocity.X) > 10)
            newAnim = "run";
        else
            newAnim = "idle";

        // Reset frame when switching animations
        if (newAnim != CurrentAnimation)
        {
            CurrentAnimation = newAnim;
            if (_sprites.Sprites.TryGetValue(newAnim, out var g))
            {
                g.Reset();
                // Squat is static: jump straight to the crouched pose
                if (newAnim == "squat")
                    g.CurrentFrame = g.Frames.Count - 1;
            }
        }

        // Advance frame of current animation (squat/attack do not loop)
        if (_sprites.Sprites.TryGetValue(CurrentAnimation, out var group))
        {
            bool hold = CurrentAnimation == "squat" || CurrentAnimation == "attack";
            group.Loop = !hold;
            group.Update(dt, AnimFps.GetValueOrDefault(CurrentAnimation, 8f));

            // Attack: hold on the last sprite, then fire the shot
            if (CurrentAnimation == "attack")
            {
                if (group.Frames.Count > 0 && group.CurrentFrame >= group.Frames.Count - 1)
                {
                    _attackFreeze -= dt;
                    if (_attackFreeze <= 0f)
                    {
                        HasPendingShot = true;
                        IsAttacking = false;
                    }
                }
            }
        }
    }

    private void ResolveCollisions(List<Platform> platforms)
    {
        OnGround = false;

        foreach (var p in platforms)
        {
            var hb = GetHitbox();
            if (!Raylib.CheckCollisionRecs(hb, p.Bounds)) continue;

            // Offset of the hitbox inside the player position
            float relX = hb.X - Position.X;
            float relY = hb.Y - Position.Y;

            float overlapLeft = (hb.X + hb.Width) - p.Bounds.X;
            float overlapRight = (p.Bounds.X + p.Bounds.Width) - hb.X;
            float overlapTop = (hb.Y + hb.Height) - p.Bounds.Y;
            float overlapBottom = (p.Bounds.Y + p.Bounds.Height) - hb.Y;

            float minOverlap = Math.Min(overlapLeft, Math.Min(overlapRight, Math.Min(overlapTop, overlapBottom)));

            if (minOverlap == overlapTop && Velocity.Y >= 0)
            {
                Position.Y = p.Bounds.Y - hb.Height - relY;
                Velocity.Y = 0;
                OnGround = true;
            }
            else if (minOverlap == overlapBottom && Velocity.Y < 0)
            {
                Position.Y = p.Bounds.Y + p.Bounds.Height - relY;
                Velocity.Y = 0;
            }
            else if (minOverlap == overlapLeft && Velocity.X > 0)
            {
                Position.X = p.Bounds.X - hb.Width - relX;
                Velocity.X = 0;
            }
            else if (minOverlap == overlapRight && Velocity.X < 0)
            {
                Position.X = p.Bounds.X + p.Bounds.Width - relX;
                Velocity.X = 0;
            }
            else
            {
                if (overlapTop < overlapBottom)
                {
                    Position.Y = p.Bounds.Y - hb.Height - relY;
                    Velocity.Y = 0;
                    OnGround = true;
                }
                else
                {
                    Position.Y = p.Bounds.Y + p.Bounds.Height - relY;
                    Velocity.Y = 0;
                }
            }
        }
    }

    public void Draw()
    {
        string anim = CurrentAnimation;
        var textures = FacingRight ? _textures : _texturesFlipped;

        if (!textures.TryGetValue(anim, out var frames) || frames.Count == 0)
            return;

        int frameIndex = 0;
        if (_sprites.Sprites.TryGetValue(anim, out var group) && group.CurrentFrame < frames.Count)
            frameIndex = group.CurrentFrame;

        var tex = frames[frameIndex];
        // Feet-anchored drawing: every frame's bottom aligns with the ground line,
        // so animations with different heights (e.g. the crouch) stay planted.
        var origin = new Vector2(Position.X, Position.Y + 64f - tex.Height);
        Raylib.DrawTextureEx(tex, origin, 0, 1f, Color.White);
    }

    public Rectangle GetHitbox()
    {
        if (IsSquatting)
            return new Rectangle(Position.X + 4, Position.Y + 28, 16, 36);
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
        DebugColor = color ?? Theme.PlatformDebug;
    }

    public void Draw()
    {
        Raylib.DrawRectangleRec(Bounds, DebugColor);
        Raylib.DrawRectangleLines((int)Bounds.X, (int)Bounds.Y, (int)Bounds.Width, (int)Bounds.Height, Color.White);
    }
}