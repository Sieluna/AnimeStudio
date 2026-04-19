using System;
using System.Collections.Generic;
using EXR.AttributeTypes;

namespace EXR;

public class EXRAttribute
{
    public string Name { get; protected set; } = string.Empty;
    public string Type { get; protected set; } = string.Empty;
    public int Size { get; protected set; }
    public object Value { get; protected set; }

    public static bool Read(EXRFile file, IEXRReader reader, out EXRAttribute attribute)
    {
        attribute = new EXRAttribute();
        return attribute.Read(file, reader);
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;

    public bool Read(EXRFile file, IEXRReader reader)
    {
        var maxLen = file.Version.MaxNameLength;

        Name = ReadHeaderToken(reader, maxLen, "name");
        if (Name.Length == 0)
        {
            return false;
        }

        Type = ReadHeaderToken(reader, maxLen, $"type for '{Name}'");
        if (Type.Length == 0)
        {
            throw new EXRFormatException($"Invalid or corrupt EXR header attribute type for '{Name}': Cannot be an empty string.");
        }

        Size = reader.ReadInt32();
        Value = ReadValue(file, reader);
        return true;
    }

    private object ReadValue(EXRFile file, IEXRReader reader)
    {
        switch (Type)
        {
            case "box2i":
                EnsureSize(16);
                return new Box2I(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            case "box2f":
                EnsureSize(16);
                return new Box2F(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            case "chromaticities":
                EnsureSize(32);
                return new Chromaticities(
                    reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle());
            case "compression":
                EnsureSize(1);
                return (EXRCompression)reader.ReadByte();
            case "double":
                EnsureSize(8);
                return reader.ReadDouble();
            case "envmap":
                EnsureSize(1);
                return (EnvMap)reader.ReadByte();
            case "float":
                EnsureSize(4);
                return reader.ReadSingle();
            case "int":
                EnsureSize(4);
                return reader.ReadInt32();
            case "keycode":
                EnsureSize(28);
                return new KeyCode(
                    reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
                    reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            case "lineOrder":
                EnsureSize(1);
                return (LineOrder)reader.ReadByte();
            case "m33f":
                EnsureSize(36);
                return new M33F(
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            case "m44f":
                EnsureSize(64);
                return new M44F(
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            case "rational":
                EnsureSize(8);
                return new Rational(reader.ReadInt32(), reader.ReadUInt32());
            case "string":
                if (Size < 0)
                {
                    throw BuildSizeException("Invalid Size");
                }

                return reader.ReadString(Size);
            case "stringvector":
                return ReadStringVector(reader);
            case "tiledesc":
                EnsureSize(9);
                return new TileDesc(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadByte());
            case "timecode":
                EnsureSize(8);
                return new TimeCode(reader.ReadUInt32(), reader.ReadUInt32());
            case "v2i":
                EnsureSize(8);
                return new V2I(reader.ReadInt32(), reader.ReadInt32());
            case "v2f":
                EnsureSize(8);
                return new V2F(reader.ReadSingle(), reader.ReadSingle());
            case "v3i":
                EnsureSize(12);
                return new V3I(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            case "v3f":
                EnsureSize(12);
                return new V3F(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            case "chlist":
            {
                var channelList = new ChannelList();
                try
                {
                    channelList.Read(file, reader, Size);
                }
                catch (Exception ex)
                {
                    throw new EXRFormatException($"Invalid or corrupt EXR header attribute '{Name}' of type chlist: {ex.Message}", ex);
                }

                return channelList;
            }
            case "preview":
            default:
                return reader.ReadBytes(Size);
        }
    }

    private List<string> ReadStringVector(IEXRReader reader)
    {
        if (Size == 0)
        {
            return [];
        }

        if (Size < 4)
        {
            throw BuildSizeException("Size must be at least 4 bytes or 0 bytes");
        }

        var values = new List<string>();
        var bytesRead = 0;
        while (bytesRead < Size)
        {
            var start = reader.Position;
            values.Add(reader.ReadString());
            bytesRead += reader.Position - start;
        }

        if (bytesRead != Size)
        {
            throw new EXRFormatException($"Invalid or corrupt EXR header attribute '{Name}' of type stringvector: Read {bytesRead} bytes but Size was {Size}.");
        }

        return values;
    }

    private static string ReadHeaderToken(IEXRReader reader, int maxLen, string segment)
    {
        try
        {
            return reader.ReadNullTerminatedString(maxLen);
        }
        catch (Exception ex)
        {
            throw new EXRFormatException($"Invalid or corrupt EXR header attribute {segment}: {ex.Message}", ex);
        }
    }

    private void EnsureSize(int expected)
    {
        if (Size != expected)
        {
            throw BuildSizeException($"Size must be {expected} bytes");
        }
    }

    private EXRFormatException BuildSizeException(string detail) =>
        new($"Invalid or corrupt EXR header attribute '{Name}' of type {Type}: {detail}, was {Size}.");
}
