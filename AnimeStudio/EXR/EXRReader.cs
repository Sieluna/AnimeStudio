using System;
using System.IO;
using System.Text;

namespace EXR;

public interface IEXRReader : IDisposable
{
    byte ReadByte();
    int ReadInt32();
    uint ReadUInt32();
    Half ReadHalf();
    float ReadSingle();
    double ReadDouble();
    string ReadNullTerminatedString(int maxLength);
    string ReadString(int length);
    string ReadString();
    byte[] ReadBytes(int count);
    void CopyBytes(byte[] dest, int offset, int count);
    int Position { get; set; }
}

public sealed class EXRReader : IEXRReader
{
    private readonly BinaryReader _reader;

    public EXRReader(Stream stream, bool leaveOpen = false)
        : this(new BinaryReader(stream, Encoding.ASCII, leaveOpen))
    {
    }

    public EXRReader(BinaryReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public byte ReadByte() => _reader.ReadByte();

    public int ReadInt32() => _reader.ReadInt32();

    public uint ReadUInt32() => _reader.ReadUInt32();

    public Half ReadHalf() => Half.ToHalf(_reader.ReadUInt16());

    public float ReadSingle() => _reader.ReadSingle();

    public double ReadDouble() => _reader.ReadDouble();

    public string ReadNullTerminatedString(int maxLength)
    {
        var start = _reader.BaseStream.Position;
        var sb = new StringBuilder();
        while (_reader.ReadByte() is var b && b != 0)
        {
            if (_reader.BaseStream.Position - start > maxLength)
            {
                throw new EXRFormatException($"Null terminated string exceeded maximum length of {maxLength} bytes.");
            }

            sb.Append((char)b);
        }

        return sb.ToString();
    }

    public string ReadString()
    {
        var length = ReadInt32();
        return ReadString(length);
    }

    public string ReadString(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
        }

        return Encoding.ASCII.GetString(_reader.ReadBytes(length));
    }

    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

    public void CopyBytes(byte[] dest, int offset, int count)
    {
        var bytesRead = _reader.BaseStream.Read(dest, offset, count);
        if (bytesRead != count)
        {
            throw new EndOfStreamException($"Expected {count} bytes, only read {bytesRead} bytes.");
        }
    }

    public int Position
    {
        get => (int)_reader.BaseStream.Position;
        set => _reader.BaseStream.Seek(value, SeekOrigin.Begin);
    }

    public void Dispose() => _reader.Dispose();
}
