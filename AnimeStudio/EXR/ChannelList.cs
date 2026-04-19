using System.Collections;
using System.Collections.Generic;

namespace EXR;

public class ChannelList : IEnumerable<Channel>
{
    public List<Channel> Channels { get; set; } = [];

    public void Read(EXRFile file, IEXRReader reader, int size)
    {
        _ = file;
        var totalSize = 0;

        while (ReadChannel(reader, out var channel, out var bytesRead))
        {
            Channels.Add(channel);
            totalSize += bytesRead;

            if (totalSize > size)
            {
                throw new EXRFormatException($"Read {totalSize} bytes but Size was {size}.");
            }
        }

        totalSize += 1;
        if (totalSize != size)
        {
            throw new EXRFormatException($"Read {totalSize} bytes but Size was {size}.");
        }
    }

    private static bool ReadChannel(IEXRReader reader, out Channel channel, out int bytesRead)
    {
        var start = reader.Position;
        var name = reader.ReadNullTerminatedString(255);
        if (name.Length == 0)
        {
            channel = null;
            bytesRead = reader.Position - start;
            return false;
        }

        channel = new Channel(
            name,
            (PixelType)reader.ReadInt32(),
            reader.ReadByte() != 0,
            reader.ReadByte(), reader.ReadByte(), reader.ReadByte(),
            reader.ReadInt32(), reader.ReadInt32());

        bytesRead = reader.Position - start;
        return true;
    }

    public IEnumerator<Channel> GetEnumerator() => Channels.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Channel this[int index]
    {
        get => Channels[index];
        set => Channels[index] = value;
    }
}
