using System.Collections;
using System.Collections.Generic;

namespace EXR;

public class OffsetTable : IEnumerable<uint>
{
    public List<uint> Offsets { get; set; }

    public OffsetTable() : this(0)
    {
    }

    public OffsetTable(int capacity)
    {
        Offsets = new List<uint>(capacity);
    }

    public void Read(IEXRReader reader, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Offsets.Add(reader.ReadUInt32());
            _ = reader.ReadUInt32();
        }
    }

    public IEnumerator<uint> GetEnumerator() => Offsets.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
