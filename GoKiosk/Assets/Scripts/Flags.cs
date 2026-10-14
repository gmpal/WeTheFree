using UnityEngine;

// Procedurally drawn flags and reticle textures (no image assets needed).
public static class Flags
{
    const int W = 512, H = 256;

    static readonly Color Blue = new Color32(1, 33, 105, 255);
    static readonly Color Red = new Color32(200, 16, 46, 255);
    static readonly Color FrBlue = new Color32(0, 85, 164, 255);
    static readonly Color FrRed = new Color32(239, 65, 53, 255);
    static readonly Color NlRed = new Color32(174, 28, 40, 255);
    static readonly Color NlBlue = new Color32(33, 70, 139, 255);

    delegate Color Pixel(float x, float y); // x in [0,1] left->right, y in [0,1] bottom->top

    static Texture2D Draw(Pixel f)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGB24, true);
        var px = new Color[W * H];
        for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
                px[j * W + i] = f((i + 0.5f) / W, (j + 0.5f) / H);
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.anisoLevel = 4;
        tex.Apply(true, true);
        return tex;
    }

    public static Texture2D France() => Draw((x, y) => x < 1 / 3f ? FrBlue : x < 2 / 3f ? Color.white : FrRed);

    public static Texture2D Netherlands() => Draw((x, y) => y > 2 / 3f ? NlRed : y > 1 / 3f ? Color.white : NlBlue);

    // Union Jack on a 60x30 grid.
    public static Texture2D UK() => Draw((u, v) =>
    {
        float x = u * 60f - 30f, y = v * 30f - 15f;           // centred
        float k = 1f / Mathf.Sqrt(5f);                        // perpendicular distance factor for slope 1/2
        float d1 = (x - 2f * y) * k;                          // signed distance to diagonal "/"
        float d2 = (x + 2f * y) * k;                          // signed distance to diagonal "\"

        if (Mathf.Abs(x) < 3f || Mathf.Abs(y) < 3f) return Red;         // red cross
        if (Mathf.Abs(x) < 5f || Mathf.Abs(y) < 5f) return Color.white; // white fimbriation

        // Counterchanged red saltire: red sits on one side of each diagonal, depending on the quadrant.
        bool upper = y > 0, right = x > 0;
        if (Mathf.Abs(d1) < 3f && upper == right)
        {
            bool redSide = right ? d1 < 0 : d1 > 0;
            if (redSide && Mathf.Abs(d1) < 2f) return Red;
            return Color.white;
        }
        if (Mathf.Abs(d2) < 3f && upper != right)
        {
            bool redSide = right ? d2 > 0 : d2 < 0;
            if (redSide && Mathf.Abs(d2) < 2f) return Red;
            return Color.white;
        }
        return Blue;
    });

    // Ring whose alpha is 1 at 12 o'clock decreasing clockwise -> radial fill via alpha cutoff.
    public static Texture2D RingTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2 - 1, y = (j + 0.5f) / size * 2 - 1;
                float r = Mathf.Sqrt(x * x + y * y);
                float angle = Mathf.Repeat(Mathf.Atan2(x, y) / (2 * Mathf.PI), 1f); // 0 at top, clockwise
                bool onRing = r > 0.7f && r < 0.95f;
                tex.SetPixel(i, j, new Color(0, 0, 0, onRing ? 1f - angle : 0f)); // black: visible on white flag stripes
            }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }

    public static Texture2D DotTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2 - 1, y = (j + 0.5f) / size * 2 - 1;
                float r2 = x * x + y * y;
                // White dot with a black outline, visible on both dark background and white stripes.
                tex.SetPixel(i, j, r2 < 0.45f ? Color.white : r2 < 1f ? Color.black : Color.clear);
            }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }
}
