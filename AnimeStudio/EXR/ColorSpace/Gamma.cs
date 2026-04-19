using System;

namespace EXR.ColorSpace;

public static class Gamma
{
    public static float Expand(float nonlinear) => (float)Math.Pow(nonlinear, 2.2);

    public static float Compress(float linear) => (float)Math.Pow(linear, 1.0 / 2.2);

    public static void Expand(ref tVec3 color)
    {
        color.X = Expand(color.X);
        color.Y = Expand(color.Y);
        color.Z = Expand(color.Z);
    }

    public static void Compress(ref tVec3 color)
    {
        color.X = Compress(color.X);
        color.Y = Compress(color.Y);
        color.Z = Compress(color.Z);
    }

    public static void Expand(ref float r, ref float g, ref float b)
    {
        r = Expand(r);
        g = Expand(g);
        b = Expand(b);
    }

    public static void Compress(ref float r, ref float g, ref float b)
    {
        r = Compress(r);
        g = Compress(g);
        b = Compress(b);
    }

    public static tVec3 Expand(float r, float g, float b)
    {
        var vec = new tVec3(r, g, b);
        Expand(ref vec);
        return vec;
    }

    public static tVec3 Compress(float r, float g, float b)
    {
        var vec = new tVec3(r, g, b);
        Compress(ref vec);
        return vec;
    }

    public static float Expand_sRGB(float nonlinear) =>
        nonlinear <= 0.04045f
            ? nonlinear / 12.92f
            : (float)Math.Pow((nonlinear + 0.055f) / 1.055f, 2.4f);

    public static float Compress_sRGB(float linear) =>
        linear <= 0.0031308f
            ? 12.92f * linear
            : 1.055f * (float)Math.Pow(linear, 1.0f / 2.4f) - 0.055f;

    public static void Expand_sRGB(ref tVec3 color)
    {
        color.X = Expand_sRGB(color.X);
        color.Y = Expand_sRGB(color.Y);
        color.Z = Expand_sRGB(color.Z);
    }

    public static void Compress_sRGB(ref tVec3 color)
    {
        color.X = Compress_sRGB(color.X);
        color.Y = Compress_sRGB(color.Y);
        color.Z = Compress_sRGB(color.Z);
    }

    public static void Expand_sRGB(ref float r, ref float g, ref float b)
    {
        r = Expand_sRGB(r);
        g = Expand_sRGB(g);
        b = Expand_sRGB(b);
    }

    public static void Compress_sRGB(ref float r, ref float g, ref float b)
    {
        r = Compress_sRGB(r);
        g = Compress_sRGB(g);
        b = Compress_sRGB(b);
    }

    public static tVec3 Expand_sRGB(float r, float g, float b)
    {
        var vec = new tVec3(r, g, b);
        Expand_sRGB(ref vec);
        return vec;
    }

    public static tVec3 Compress_sRGB(float r, float g, float b)
    {
        var vec = new tVec3(r, g, b);
        Compress_sRGB(ref vec);
        return vec;
    }
}
