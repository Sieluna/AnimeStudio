using System;
using EXR.AttributeTypes;

// source http://www.ryanjuckett.com/programming/rgb-color-space-conversion/
namespace EXR.ColorSpace;

public struct tVec2
{
    public float X;
    public float Y;

    public tVec2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct tVec3
{
    public float X;
    public float Y;
    public float Z;

    public tVec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public struct tMat3x3
{
    public float M00, M01, M02;
    public float M10, M11, M12;
    public float M20, M21, M22;

    public void SetCol(int colIdx, tVec3 vec)
    {
        this[0, colIdx] = vec.X;
        this[1, colIdx] = vec.Y;
        this[2, colIdx] = vec.Z;
    }

    public bool Invert(out tMat3x3 result)
    {
        result = default;

        var minor00 = this[1, 1] * this[2, 2] - this[1, 2] * this[2, 1];
        var minor01 = this[1, 2] * this[2, 0] - this[1, 0] * this[2, 2];
        var minor02 = this[1, 0] * this[2, 1] - this[1, 1] * this[2, 0];

        var determinant = this[0, 0] * minor00 + this[0, 1] * minor01 + this[0, 2] * minor02;
        if (determinant > -0.000001f && determinant < 0.000001f)
        {
            return false;
        }

        var invDet = 1.0f / determinant;
        result[0, 0] = invDet * minor00;
        result[0, 1] = invDet * (this[2, 1] * this[0, 2] - this[2, 2] * this[0, 1]);
        result[0, 2] = invDet * (this[0, 1] * this[1, 2] - this[0, 2] * this[1, 1]);

        result[1, 0] = invDet * minor01;
        result[1, 1] = invDet * (this[2, 2] * this[0, 0] - this[2, 0] * this[0, 2]);
        result[1, 2] = invDet * (this[0, 2] * this[1, 0] - this[0, 0] * this[1, 2]);

        result[2, 0] = invDet * minor02;
        result[2, 1] = invDet * (this[2, 0] * this[0, 1] - this[2, 1] * this[0, 0]);
        result[2, 2] = invDet * (this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0]);

        return true;
    }

    public static tVec3 operator *(tMat3x3 mat, tVec3 vec) => new(
        mat[0, 0] * vec.X + mat[0, 1] * vec.Y + mat[0, 2] * vec.Z,
        mat[1, 0] * vec.X + mat[1, 1] * vec.Y + mat[1, 2] * vec.Z,
        mat[2, 0] * vec.X + mat[2, 1] * vec.Y + mat[2, 2] * vec.Z);

    public float this[int row, int col]
    {
        get => (row, col) switch
        {
            (0, 0) => M00,
            (0, 1) => M01,
            (0, 2) => M02,
            (1, 0) => M10,
            (1, 1) => M11,
            (1, 2) => M12,
            (2, 0) => M20,
            (2, 1) => M21,
            (2, 2) => M22,
            _ => throw new ArgumentOutOfRangeException()
        };
        set
        {
            switch (row, col)
            {
                case (0, 0): M00 = value; break;
                case (0, 1): M01 = value; break;
                case (0, 2): M02 = value; break;
                case (1, 0): M10 = value; break;
                case (1, 1): M11 = value; break;
                case (1, 2): M12 = value; break;
                case (2, 0): M20 = value; break;
                case (2, 1): M21 = value; break;
                case (2, 2): M22 = value; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

public static class XYZ
{
    public static tMat3x3 CalcColorSpaceConversion_RGB_to_XYZ(Chromaticities chromaticities) =>
        CalcColorSpaceConversion_RGB_to_XYZ(
            new tVec2(chromaticities.RedX, chromaticities.RedY),
            new tVec2(chromaticities.GreenX, chromaticities.GreenY),
            new tVec2(chromaticities.BlueX, chromaticities.BlueY),
            new tVec2(chromaticities.WhiteX, chromaticities.WhiteY));

    public static tMat3x3 CalcColorSpaceConversion_RGB_to_XYZ(
        tVec2 red_xy,
        tVec2 green_xy,
        tVec2 blue_xy,
        tVec2 white_xy)
    {
        var output = new tMat3x3();

        var r = new tVec3(red_xy.X, red_xy.Y, 1.0f - (red_xy.X + red_xy.Y));
        var g = new tVec3(green_xy.X, green_xy.Y, 1.0f - (green_xy.X + green_xy.Y));
        var b = new tVec3(blue_xy.X, blue_xy.Y, 1.0f - (blue_xy.X + blue_xy.Y));
        var w = new tVec3(white_xy.X, white_xy.Y, 1.0f - (white_xy.X + white_xy.Y));

        w.X /= white_xy.Y;
        w.Y /= white_xy.Y;
        w.Z /= white_xy.Y;

        output.SetCol(0, r);
        output.SetCol(1, g);
        output.SetCol(2, b);

        output.Invert(out var invMat);
        var scale = invMat * w;

        output[0, 0] *= scale.X;
        output[1, 0] *= scale.X;
        output[2, 0] *= scale.X;

        output[0, 1] *= scale.Y;
        output[1, 1] *= scale.Y;
        output[2, 1] *= scale.Y;

        output[0, 2] *= scale.Z;
        output[1, 2] *= scale.Z;
        output[2, 2] *= scale.Z;

        return output;
    }
}
