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
    static readonly Color DeRed = new Color32(221, 0, 0, 255);
    static readonly Color DeGold = new Color32(255, 206, 0, 255);
    static readonly Color EsRed = new Color32(170, 21, 27, 255);
    static readonly Color EsYellow = new Color32(241, 191, 0, 255);

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

    static Color Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

    // Flag for a video's language code (the file name, e.g. ENG.mp4), or null if there is none built in.
    // Language-specific: one flag per language, not per country.
    public static Texture2D For(string code)
    {
        switch (code)
        {
            case "ENG": return UK();
            case "FRA": return France();
            case "NED": return Netherlands();
            case "DEU": return Germany();
            case "SPA": return Spain();
            case "ITA": return VBands(Hex(0x009246), Color.white, Hex(0xCE2B37));
            case "POR": return Portugal();
            case "POL": return HBands(Color.white, Hex(0xDC143C));
            case "CES": return Czech();
            case "HUN": return HBands(Hex(0xCE2939), Color.white, Hex(0x477050));
            case "RON": return VBands(Hex(0x002B7F), Hex(0xFCD116), Hex(0xCE1126));
            case "BUL": return HBands(Color.white, Hex(0x00966E), Hex(0xD62612));
            case "HRV": return Croatia();
            case "GRE": return Greece();
            case "SWE": return Nordic(Hex(0x006AA7), Hex(0xFECC02));
            case "DAN": return Nordic(Hex(0xC8102E), Color.white);
            case "NOR": return Nordic(Hex(0xBA0C2F), Color.white, Hex(0x00205B));
            case "FIN": return Nordic(Color.white, Hex(0x002F6C));
            case "ISL": return Nordic(Hex(0x02529C), Color.white, Hex(0xDC1E35));
            case "EST": return HBands(Hex(0x0072CE), Color.black, Color.white);
            case "LAV": return Draw((x, y) => Mathf.Abs(y - 0.5f) < 0.1f ? Color.white : Hex(0x9E3039));
            case "LIT": return HBands(Hex(0xFDB913), Hex(0x006A44), Hex(0xC1272D));
            case "UKR": return HBands(Hex(0x0057B7), Hex(0xFFD700));
            case "GLE": return VBands(Hex(0x169B62), Color.white, Hex(0xFF883E));
            case "LTZ": return HBands(Hex(0xEF3340), Color.white, Hex(0x00A3E0));
            case "CAT": return Draw((x, y) => (int)(y * 9) % 2 == 1 ? Hex(0xDA121A) : Hex(0xFCDD09));
            default: return null;
        }
    }

    // Plain light grey; the kiosk writes the language code on it.
    public static Texture2D Unknown() => Draw((x, y) => new Color(0.85f, 0.85f, 0.85f));

    // Equal horizontal stripes, listed top to bottom.
    static Texture2D HBands(params Color[] c) => Draw((x, y) => HBandColour(y, c));

    // Equal vertical stripes, listed left to right.
    static Texture2D VBands(params Color[] c) => Draw((x, y) => c[Mathf.Min(c.Length - 1, (int)(x * c.Length))]);

    // Scandinavian cross, shifted towards the hoist; optional inner cross (Norway, Iceland).
    static Texture2D Nordic(Color field, Color cross, Color? inner = null) => Draw((x, y) =>
    {
        float dx = Mathf.Abs(x - 0.375f) * 2f, dy = Mathf.Abs(y - 0.5f); // dx in flag-height units (2:1 flag)
        if (inner.HasValue && (dx < 0.0625f || dy < 0.0625f)) return inner.Value;
        if (dx < 0.125f || dy < 0.125f) return cross;
        return field;
    });

    public static Texture2D France() => VBands(FrBlue, Color.white, FrRed);

    public static Texture2D Netherlands() => HBands(NlRed, Color.white, NlBlue);

    public static Texture2D Germany() => HBands(Color.black, DeRed, DeGold);

    // Red-yellow-red with the yellow band twice as tall (civil flag, no coat of arms).
    public static Texture2D Spain() => Draw((x, y) => y > 0.75f || y < 0.25f ? EsRed : EsYellow);

    // Green and red with a simplified yellow armillary ring (no shield).
    static Texture2D Portugal() => Draw((x, y) =>
    {
        float dx = (x - 0.4f) * 2f, dy = y - 0.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
        if (r > 0.17f && r < 0.23f) return Hex(0xFFE000);
        return x < 0.4f ? Hex(0x006600) : Hex(0xFF0000);
    });

    // White over red with a blue triangle at the hoist.
    static Texture2D Czech() => Draw((x, y) =>
        x * 2f < 0.5f - Mathf.Abs(y - 0.5f) ? Hex(0x11457E) : y > 0.5f ? Color.white : Hex(0xD7141A));

    // Red-white-blue with a simplified red-and-white chequered shield.
    static Texture2D Croatia() => Draw((x, y) =>
    {
        float sx = (x - 0.5f) * 2f, sy = y - 0.5f; // flag-height units
        if (Mathf.Abs(sx) < 0.15f && Mathf.Abs(sy) < 0.2f)
        {
            int cx = Mathf.FloorToInt((sx + 0.15f) / 0.06f), cy = Mathf.FloorToInt((sy + 0.2f) / 0.08f);
            return (cx + cy) % 2 == 0 ? Hex(0xFF0000) : Color.white;
        }
        return HBandColour(y, Hex(0xFF0000), Color.white, Hex(0x171796));
    });

    static Color HBandColour(float y, params Color[] c) => c[Mathf.Min(c.Length - 1, (int)((1 - y) * c.Length))];

    // Nine blue and white stripes with a white cross on a blue canton.
    static Texture2D Greece() => Draw((x, y) =>
    {
        var blue = Hex(0x0D5EAF);
        int stripe = Mathf.Min(8, (int)((1 - y) * 9));
        float cx = x * 2f, cy = 1 - y; // flag-height units from the top-left
        if (cx < 5 / 9f && cy < 5 / 9f)
            return Mathf.Abs(cx - 2.5f / 9f) < 0.5f / 9f || Mathf.Abs(cy - 2.5f / 9f) < 0.5f / 9f ? Color.white : blue;
        return stripe % 2 == 0 ? blue : Color.white;
    });

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
