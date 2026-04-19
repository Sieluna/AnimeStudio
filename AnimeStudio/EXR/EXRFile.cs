using System;
using System.Collections.Generic;
using System.IO;

namespace EXR;

public class EXRFile
{
    public EXRVersion Version { get; protected set; }
    public List<EXRHeader> Headers { get; protected set; } = [];
    public List<OffsetTable> OffsetTables { get; protected set; } = [];
    public List<EXRPart> Parts { get; protected set; } = [];

    public void Read(IEXRReader reader)
    {
        if (reader.ReadInt32() != 20000630)
        {
            throw new EXRFormatException("Invalid or corrupt EXR layout: First four bytes were not 20000630.");
        }

        Version = new EXRVersion(reader.ReadInt32());

        Headers = [];
        if (Version.IsMultiPart)
        {
            while (true)
            {
                var header = new EXRHeader();
                header.Read(this, reader);
                if (header.IsEmpty)
                {
                    break;
                }

                Headers.Add(header);
            }

            throw new NotImplementedException("Multi part EXR files are not currently supported");
        }

        if (Version.IsSinglePartTiled)
        {
            throw new NotImplementedException("Tiled EXR files are not currently supported");
        }

        var singleHeader = new EXRHeader();
        singleHeader.Read(this, reader);
        Headers.Add(singleHeader);

        OffsetTables = [];
        foreach (var header in Headers)
        {
            int offsetTableSize = Version.IsMultiPart
                ? header.ChunkCount
                : Version.IsSinglePartTiled
                    ? 0
                    : (int)Math.Ceiling(header.DataWindow.Height / (double)GetScanLinesPerBlock(header.Compression));

            var table = new OffsetTable(offsetTableSize);
            table.Read(reader, offsetTableSize);
            OffsetTables.Add(table);
        }
    }

    public static int GetScanLinesPerBlock(EXRCompression compression) => compression switch
    {
        EXRCompression.ZIP or EXRCompression.PXR24 => 16,
        EXRCompression.PIZ or EXRCompression.B44 or EXRCompression.B44A => 32,
        _ => 1
    };

    public static int GetBytesPerPixel(ImageDestFormat format) => format switch
    {
        ImageDestFormat.RGB16 or ImageDestFormat.BGR16 => 6,
        ImageDestFormat.RGB32 or ImageDestFormat.BGR32 => 12,
        ImageDestFormat.RGB8 or ImageDestFormat.BGR8 => 3,
        ImageDestFormat.PremultipliedRGBA16 or ImageDestFormat.PremultipliedBGRA16 or ImageDestFormat.RGBA16 or ImageDestFormat.BGRA16 => 8,
        ImageDestFormat.PremultipliedRGBA32 or ImageDestFormat.PremultipliedBGRA32 or ImageDestFormat.RGBA32 or ImageDestFormat.BGRA32 => 16,
        ImageDestFormat.PremultipliedRGBA8 or ImageDestFormat.PremultipliedBGRA8 or ImageDestFormat.RGBA8 or ImageDestFormat.BGRA8 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unrecognized destination format")
    };

    public static int GetBitsPerPixel(ImageDestFormat format) => format switch
    {
        ImageDestFormat.PremultipliedRGBA32 or ImageDestFormat.PremultipliedBGRA32 or ImageDestFormat.RGBA32 or ImageDestFormat.BGRA32 or ImageDestFormat.RGB32 or ImageDestFormat.BGR32 => 32,
        ImageDestFormat.PremultipliedRGBA8 or ImageDestFormat.PremultipliedBGRA8 or ImageDestFormat.RGBA8 or ImageDestFormat.BGRA8 or ImageDestFormat.RGB8 or ImageDestFormat.BGR8 => 8,
        ImageDestFormat.RGB16 or ImageDestFormat.BGR16 or ImageDestFormat.PremultipliedRGBA16 or ImageDestFormat.PremultipliedBGRA16 or ImageDestFormat.RGBA16 or ImageDestFormat.BGRA16 => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unrecognized destination format")
    };

#if DOTNET
    public static EXRFile FromFile(string file)
    {
        using var reader = new EXRReader(new FileStream(file, FileMode.Open, FileAccess.Read));
        return FromReader(reader);
    }
#endif

    public static EXRFile FromStream(Stream stream)
    {
        using var reader = new EXRReader(new BinaryReader(stream));
        return FromReader(reader);
    }

    public static EXRFile FromReader(IEXRReader reader)
    {
        var image = new EXRFile();
        image.Read(reader);

        image.Parts = [];
        for (var i = 0; i < image.Headers.Count; i++)
        {
            image.Parts.Add(new EXRPart(image.Version, image.Headers[i], image.OffsetTables[i]));
        }

        return image;
    }
}
