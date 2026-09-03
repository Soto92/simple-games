using System.Numerics;
using Raylib_cs;

namespace ElementalSoldier.Game;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Radius;
    public Color Color;
    public bool Dead;

    public Bullet(Vector2 position, Vector2 velocity, Color color, float radius = 5f)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        Radius = radius;
    }

    public void Update(float dt, List<Platform> platforms)
    {
        Position += Velocity * dt;

        foreach (var p in platforms)
        {
            if (Raylib.CheckCollisionCircleRec(Position, Radius, p.Bounds))
            {
                Dead = true;
                return;
            }
        }

        if (Position.X < -200 || Position.X > 12200 || Position.Y < -200 || Position.Y > 900)
            Dead = true;
    }

    public void Draw()
    {
        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, Color);
    }
}