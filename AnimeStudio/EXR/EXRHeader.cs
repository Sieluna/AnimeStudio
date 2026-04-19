using System;
using System.Collections.Generic;
using EXR.AttributeTypes;

namespace EXR;

public class EXRHeader
{
    public static readonly Chromaticities DefaultChromaticities = new(
        0.6400f, 0.3300f,
        0.3000f, 0.6000f,
        0.1500f, 0.0600f,
        0.3127f, 0.3290f);

    public Dictionary<string, EXRAttribute> Attributes { get; } = new(StringComparer.Ordinal);

    public void Read(EXRFile file, IEXRReader reader)
    {
        while (EXRAttribute.Read(file, reader, out var attribute))
        {
            Attributes[attribute.Name] = attribute;
        }
    }

    public bool TryGetAttribute<T>(string name, out T result)
    {
        if (!Attributes.TryGetValue(name, out var attr))
        {
            result = default;
            return false;
        }

        if (attr.Value is T typed)
        {
            result = typed;
            return true;
        }

        if (attr.Value is null && default(T) is null)
        {
            result = default;
            return true;
        }

        result = default;
        return false;
    }

    public bool IsEmpty => Attributes.Count == 0;

    public int ChunkCount => GetRequiredAttribute<int>("chunkCount");

    public Box2I DataWindow => GetRequiredAttribute<Box2I>("dataWindow");

    public EXRCompression Compression => GetRequiredAttribute<EXRCompression>("compression");

    public PartType Type => GetRequiredAttribute<PartType>("type");

    public ChannelList Channels => GetRequiredAttribute<ChannelList>("channels");

    public Chromaticities Chromaticities
    {
        get
        {
            foreach (var attr in Attributes.Values)
            {
                if (attr.Type == "chromaticities" && attr.Value is Chromaticities chromaticities)
                {
                    return chromaticities;
                }
            }

            return DefaultChromaticities;
        }
    }

    private T GetRequiredAttribute<T>(string name)
    {
        if (!TryGetAttribute<T>(name, out var value))
        {
            throw new EXRFormatException($"Invalid or corrupt EXR header: Missing {name} attribute.");
        }

        return value;
    }
}
