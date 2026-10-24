namespace Incognito
{
    /// <summary>Engine-free 2D vector for the rules (replaces <c>UnityEngine.Vector2</c>).</summary>
    public readonly struct Vec2 : System.IEquatable<Vec2>
    {
        public readonly float x;
        public readonly float y;

        public Vec2(float x, float y) { this.x = x; this.y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.x * s, a.y * s);

        public bool Equals(Vec2 o) => x == o.x && y == o.y;
        public override bool Equals(object obj) => obj is Vec2 o && Equals(o);
        public override int GetHashCode() => System.HashCode.Combine(x, y);
        public override string ToString() => $"({x}, {y})";
    }

    /// <summary>Engine-free integer 2D vector (replaces <c>UnityEngine.Vector2Int</c>).</summary>
    public readonly struct Vec2Int : System.IEquatable<Vec2Int>
    {
        public readonly int x;
        public readonly int y;

        public Vec2Int(int x, int y) { this.x = x; this.y = y; }

        public bool Equals(Vec2Int o) => x == o.x && y == o.y;
        public override bool Equals(object obj) => obj is Vec2Int o && Equals(o);
        public override int GetHashCode() => System.HashCode.Combine(x, y);
        public override string ToString() => $"({x}, {y})";
    }

    /// <summary>Engine-free axis-aligned rectangle (replaces <c>UnityEngine.Rect</c>).</summary>
    public readonly struct Rect2
    {
        public readonly float x;
        public readonly float y;
        public readonly float width;
        public readonly float height;

        public Rect2(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }

        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
    }
}
