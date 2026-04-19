using EXR;

namespace EXR.AttributeTypes;

public readonly record struct Box2F(float XMin, float YMin, float XMax, float YMax)
{
    public float Width => XMax - XMin + 1;
    public float Height => YMax - YMin + 1;
}

public readonly record struct Box2I(int XMin, int YMin, int XMax, int YMax)
{
    public int Width => XMax - XMin + 1;
    public int Height => YMax - YMin + 1;
}

public readonly record struct Chromaticities(
    float RedX,
    float RedY,
    float GreenX,
    float GreenY,
    float BlueX,
    float BlueY,
    float WhiteX,
    float WhiteY);

public readonly record struct KeyCode(
    int FilmMfcCode,
    int FilmType,
    int Prefix,
    int Count,
    int PerfOffset,
    int PerfsPerFrame,
    int PerfsPerCount);

public readonly record struct M33F(float[] Values)
{
    public M33F(float v0, float v1, float v2, float v3, float v4, float v5, float v6, float v7, float v8)
        : this([v0, v1, v2, v3, v4, v5, v6, v7, v8])
    {
    }
}

public readonly record struct M44F(float[] Values)
{
    public M44F(
        float v0, float v1, float v2, float v3,
        float v4, float v5, float v6, float v7,
        float v8, float v9, float v10, float v11,
        float v12, float v13, float v14, float v15)
        : this([v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15])
    {
    }
}

public readonly record struct Rational(int Numerator, uint Denominator)
{
    public double Value => (double)Numerator / Denominator;
}

public readonly record struct TileDesc(uint XSize, uint YSize, LevelMode LevelMode, RoundingMode RoundingMode)
{
    public TileDesc(uint xSize, uint ySize, byte mode)
        : this(
            xSize,
            ySize,
            (LevelMode)(mode & 0xF),
            (RoundingMode)((mode & 0xF0) >> 4))
    {
    }
}

public readonly record struct TimeCode(uint TimeAndFlags, uint UserData);

public readonly record struct V2F(float V0, float V1)
{
    public float X => V0;
    public float Y => V1;
}

public readonly record struct V2I(int V0, int V1)
{
    public int X => V0;
    public int Y => V1;
}

public readonly record struct V3F(float V0, float V1, float V2)
{
    public float X => V0;
    public float Y => V1;
    public float Z => V2;
}

public readonly record struct V3I(int V0, int V1, int V2)
{
    public int X => V0;
    public int Y => V1;
    public int Z => V2;
}

public enum EnvMap
{
    LatLong = 0,
    Cube = 1
}
