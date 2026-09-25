using UnityEngine;

// Runtime-generated art for abilities that are animated in code instead of being
// authored as a sprite asset. Nothing here loads from Resources: the textures are
// built pixel by pixel the first time an ability asks for them and cached for the
// life of the process, so repeat casts cost nothing.
public static class ProceduralFx
{
    private static Sprite iceShard;
    private static Sprite[] flameFrames;
    private static Sprite ringOutline;
    private static Sprite puddle;
    private static Sprite mudGlob;
    private static Sprite shadow;
    private static Sprite meteor;
    private static Sprite burst;

    // Palette sampled from the hand-drawn fireball sheet in
    // Assets/Textures/Attacks/FireWizard/Fireball: a bright yellow core, an amber
    // body, an orange tongue and a darker orange accent at the outline.
    private static readonly Color FireCore = new Color(0.96f, 0.87f, 0.12f);
    private static readonly Color FireAmber = new Color(0.91f, 0.64f, 0.23f);
    private static readonly Color FireOrange = new Color(0.94f, 0.53f, 0.17f);
    private static readonly Color FireAccent = new Color(0.72f, 0.36f, 0.12f);
    private const int FlameFrameCount = 6;

    // A tapered ice shard, pivoted near its base so it can be scaled outward from
    // the point it was spawned at.
    public static Sprite IceShard => iceShard != null ? iceShard : (iceShard = BuildIceShard());

    // A looping flame animation. Cycle these frames to make fire actually move
    // instead of scaling one static shape.
    public static Sprite[] FlameFrames => flameFrames != null ? flameFrames : (flameFrames = BuildFlameFrames());

    // First frame, for callers that only want a still flame.
    public static Sprite Flame => FlameFrames[0];

    // A soft dark ellipse cast on the ground by something falling.
    public static Sprite Shadow => shadow != null ? shadow : (shadow = BuildShadow());

    // A burning ball with a tail, for the meteor itself.
    public static Sprite Meteor => meteor != null ? meteor : (meteor = BuildMeteor());

    // A filled blast disc, for an explosion radius.
    public static Sprite Burst => burst != null ? burst : (burst = BuildBurst());

    // A hollow ring used to telegraph an area before it ignites.
    public static Sprite RingOutline => ringOutline != null ? ringOutline : (ringOutline = BuildRingOutline());

    // A flat, wet-looking ellipse. Several of these gather into an ice patch.
    public static Sprite Puddle => puddle != null ? puddle : (puddle = BuildPuddle());

    // A wet clod of earth thrown up by the mud patch spurt.
    public static Sprite MudGlob => mudGlob != null ? mudGlob : (mudGlob = BuildMudGlob());

    private static Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Sprite BuildIceShard()
    {
        const int width = 32, height = 64;
        Texture2D texture = NewTexture(width, height);
        for (int y = 0; y < height; y++)
        {
            float along = (y + 0.5f) / height;
            float halfWidth = Mathf.Sin(along * Mathf.PI) * 0.5f;
            for (int x = 0; x < width; x++)
            {
                float across = (x + 0.5f) / width - 0.5f;
                float edge = halfWidth <= 0.0001f ? 1f : Mathf.Abs(across) / halfWidth;
                float solid = Mathf.Clamp01(1f - edge);
                solid *= solid;
                Color tint = Color.Lerp(new Color(0.35f, 0.68f, 1f), Color.white, solid);
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, solid * Mathf.Lerp(0.55f, 1f, along)));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.15f), 64f);
    }

    private static Sprite[] BuildFlameFrames()
    {
        Sprite[] frames = new Sprite[FlameFrameCount];
        for (int i = 0; i < FlameFrameCount; i++) frames[i] = BuildFlameFrame(i / (float)FlameFrameCount);
        return frames;
    }

    // One frame of the flame loop. The silhouette is a half-round body with two
    // travelling waves running across it, so successive frames grow and drop
    // different tongues the way the fireball sheet does. Colour comes from how deep
    // inside the silhouette a pixel sits, giving the flat banded look rather than a
    // gradient.
    private static Sprite BuildFlameFrame(float phase)
    {
        const int width = 64, height = 96;
        const float tau = Mathf.PI * 2f;
        Texture2D texture = NewTexture(width, height);
        for (int x = 0; x < width; x++)
        {
            float across = (x + 0.5f) / width * 2f - 1f;
            // A full, broad body. A square-root envelope with fast waves broke the
            // flame into thin tongues that were mostly soft edge, which is what made
            // it read as translucent however opaque the pixels were.
            float envelope = Mathf.Pow(Mathf.Max(0f, 1f - across * across), 0.35f);
            float lick = 0.16f * Mathf.Sin(across * 5f + phase * tau) + 0.08f * Mathf.Sin(across * 9f - phase * tau * 1.6f);
            float top = envelope * (0.94f + lick);
            for (int y = 0; y < height; y++)
            {
                float up = (y + 0.5f) / height;
                float depth = Mathf.Min((top - up) / 0.45f, (1f - Mathf.Abs(across)) / 0.3f);
                // The bright core sits low in the flame and burns out towards the tip.
                float coreness = Mathf.Clamp01(depth) * Mathf.Lerp(1.2f, 0.45f, up);
                Color tint = coreness > 0.6f ? FireCore : coreness > 0.34f ? FireAmber : coreness > 0.12f ? FireOrange : FireAccent;
                // Fully opaque body, with at most one pixel of feathering right at the
                // silhouette. Transparent pixels still carry the flame's colour so
                // bilinear filtering never blends towards transparent black and leaves
                // a dark, washed-out fringe around the edge.
                float alpha = 0f;
                if (up <= top)
                {
                    float toEdge = (top - up) * height;
                    alpha = toEdge < 1.5f ? Mathf.Clamp01(toEdge / 1.5f) : 1f;
                }
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, alpha));
            }
        }
        texture.Apply();
        // Pivot at the base so a flame stands on the ground and licks upward.
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.02f), 64f);
    }

    private static Sprite BuildShadow()
    {
        const int width = 96, height = 48;
        Texture2D texture = NewTexture(width, height);
        float halfWidth = width * 0.5f, halfHeight = height * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f - halfWidth) / halfWidth;
                float dy = (y + 0.5f - halfHeight) / halfHeight;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float solid = Mathf.Clamp01(1f - distance);
                solid = solid * solid;
                texture.SetPixel(x, y, new Color(0.05f, 0.04f, 0.03f, solid * 0.75f));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite BuildMeteor()
    {
        const int size = 96;
        Texture2D texture = NewTexture(size, size);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                // The head sits low; the tail streams upward behind it as it falls.
                float head = Mathf.Sqrt(dx * dx + (dy + 0.35f) * (dy + 0.35f)) / 0.55f;
                float tailWidth = Mathf.Max(0.06f, 0.34f * (1f - Mathf.Clamp01((dy - 0.1f) / 0.9f)));
                float tail = dy > -0.35f ? Mathf.Abs(dx) / tailWidth : 999f;
                float shape = Mathf.Min(head, tail);
                if (shape > 1f) { texture.SetPixel(x, y, Color.clear); continue; }
                float coverage = 1f - Mathf.SmoothStep(0.93f, 1f, shape);
                Color tint = shape < 0.35f ? FireCore : shape < 0.6f ? FireAmber : shape < 0.85f ? FireOrange : FireAccent;
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, coverage));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.28f), 64f);
    }

    private static Sprite BuildBurst()
    {
        const int size = 128;
        Texture2D texture = NewTexture(size, size);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float angle = Mathf.Atan2(dy, dx);
                // A ragged rim so the blast does not read as a plain circle.
                float ragged = 1f + 0.06f * Mathf.Sin(angle * 11f) + 0.04f * Mathf.Sin(angle * 19f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / ragged;
                if (distance > 1f) { texture.SetPixel(x, y, Color.clear); continue; }
                float coverage = 1f - Mathf.SmoothStep(0.94f, 1f, distance);
                Color tint = distance < 0.4f ? FireCore : distance < 0.68f ? FireAmber : distance < 0.88f ? FireOrange : FireAccent;
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, coverage));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite BuildRingOutline()
    {
        const int size = 128;
        Texture2D texture = NewTexture(size, size);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                // Two flat rings centred on 0.85 of the radius, to match the crisp
                // linework of the fire art rather than glowing softly.
                float band = Mathf.Abs(distance - 0.85f);
                float coverage = 1f - Mathf.SmoothStep(0.1f, 0.125f, band);
                if (coverage <= 0f) { texture.SetPixel(x, y, Color.clear); continue; }
                Color tint = band < 0.045f ? FireCore : FireOrange;
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, coverage));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite BuildPuddle()
    {
        const int width = 64, height = 32;
        Texture2D texture = NewTexture(width, height);
        float halfWidth = width * 0.5f, halfHeight = height * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f - halfWidth) / halfWidth;
                float dy = (y + 0.5f - halfHeight) / halfHeight;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float solid = Mathf.Clamp01(1f - distance);
                solid = solid * solid * (3f - 2f * solid);
                // A brighter rim reads as a wet edge catching the light.
                float rim = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) / 0.22f);
                Color tint = Color.Lerp(new Color(0.55f, 0.78f, 0.95f), new Color(0.88f, 0.96f, 1f), rim);
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, solid * 0.85f));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite BuildMudGlob()
    {
        const int size = 32;
        Texture2D texture = NewTexture(size, size);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float solid = Mathf.Clamp01(1f - distance);
                solid = solid * solid * (3f - 2f * solid);
                // Lighter towards the top so the clod reads as lit from above.
                float lift = Mathf.Clamp01((dy + 1f) * 0.5f);
                Color tint = Color.Lerp(new Color(0.24f, 0.15f, 0.07f), new Color(0.48f, 0.33f, 0.16f), lift);
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, solid));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // True when the point is inside something solid the player cannot walk through:
    // an arena wall, a scattered obstacle, or a raised rock wall. Solid geometry in
    // this project is a non-trigger collider on an object that is not a player.
    public static bool IsInsideSolid(Vector2 point, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger) continue;
            Transform root = hit.transform.root;
            if (root.CompareTag("Player") || root.CompareTag("destroyableAttack")) continue;
            return true;
        }
        return false;
    }
}
