namespace EXR;

public class Channel
{
    public string Name { get; set; }
    public PixelType Type { get; set; }
    public bool Linear { get; set; }
    public int XSampling { get; set; }
    public int YSampling { get; set; }
    public byte[] Reserved { get; set; }

    public Channel(string name, PixelType type, bool linear, int xSampling, int ySampling)
        : this(name, type, linear, 0, 0, 0, xSampling, ySampling)
    {
    }

    public Channel(string name, PixelType type, bool linear, byte reserved0, byte reserved1, byte reserved2, int xSampling, int ySampling)
    {
        Name = name;
        Type = type;
        Linear = linear;
        XSampling = xSampling;
        YSampling = ySampling;
        Reserved = [reserved0, reserved1, reserved2];
    }

    public override string ToString() => $"{GetType().Name} {Name} {Type}";
}
