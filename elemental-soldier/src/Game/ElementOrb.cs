using System.Numerics;
using Raylib_cs;

namespace ElementalSoldier.Game;

public class ElementOrb
{
    public Vector2 Position;
    public Element Element;
    public bool Collected;
    public float Radius = 12f;
    private float _phase;

    public ElementOrb(Vector2 position, Element element)
    {
        Position = position;
        Element = element;
        _phase = position.X * 0.01f;
    }

    public Color Color => Element switch
    {
        Element.Fire => Theme.ElementFire,
        Element.Ice => Theme.ElementIce,
        Element.Electric => Theme.ElementElectric,
        _ => Theme.ElementNeutral,
    };

    public void Update(float dt)
    {
        _phase += dt;
    }

    // Checks if the absorb beam segment crosses this orb
    public bool BeamHits(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float lenSq = delta.LengthSquared();
        if (lenSq < 0.0001f) return false;

        float t = Vector2.Dot(Position - from, delta) / lenSq;
        t = Math.Clamp(t, 0f, 1f);
        Vector2 closest = from + delta * t;
        return (closest - Position).LengthSquared() <= Radius * Radius;
    }

    public void Draw()
    {
        float bobY = MathF.Sin(_phase * 2f) * 3f;
        var center = new Vector2(Position.X, Position.Y + bobY);
        var color = Color;

        // Outer glow
        Raylib.DrawCircleV(center, Radius + 9f, new Color(color.R, color.G, color.B, (byte)40));
        // Solid body
        Raylib.DrawCircleV(center, Radius, color);
        // Bright core
        Raylib.DrawCircleV(center, Radius * 0.45f, Color.White);
    }
}