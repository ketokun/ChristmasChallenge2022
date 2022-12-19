namespace KayakDotNetChristmasChallenge2022;

public class IpWithIndex : IComparable<IpWithIndex>
{
    public uint LowIp { get; }
    public uint FileIndex { get; }

    public IpWithIndex(uint lowIp, uint fileIndex)
    {
        LowIp = lowIp;
        FileIndex = fileIndex;
    }

    public int CompareTo(IpWithIndex? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return LowIp.CompareTo(other.LowIp);
    }
};