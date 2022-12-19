using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace KayakDotNetChristmasChallenge2022;

public struct IpLocationData
{
    internal static (IpRange result, Location location) ParserRow(string row)
    {
        var separators = new [] {"\",\""};
        var rowComponents = row.Split(separators, StringSplitOptions.None);

        rowComponents[0] = rowComponents[0][1..];
        rowComponents[7] = rowComponents[7][..^1];

        var result = new IpRange
        {
            LowValue = uint.Parse(rowComponents[0]),
            HiValue = uint.Parse(rowComponents[1]),
        };
        var location = new Location
        {
            CountryCode = rowComponents[2],
            Country = rowComponents[3],
            Region = rowComponents[4],
            City = rowComponents[5],
            Lat = double.Parse(rowComponents[6]),
            Lon = double.Parse(rowComponents[7]),
        };
        return (result, location);
    }
}

public class Location
{
    public string CountryCode, Country, Region, City;
    public double Lat, Lon;

    public override bool Equals(object? obj)
    {
        return obj != null && Equals((Location) obj);
    }

    private bool Equals(Location other)
    {
        return CountryCode == other.CountryCode && Country == other.Country && Region == other.Region &&
               City == other.City && Lat.Equals(other.Lat) && Lon.Equals(other.Lon);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CountryCode, Country, Region, City, Lat, Lon);
    }
}

public struct IpRange : IComparable<IpRange>
{
    public uint LowValue, HiValue;
    public int LocationID;

    public int CompareTo(IpRange other)
    {
        var lowValueComparison = LowValue.CompareTo(other.LowValue);
        if (lowValueComparison > 0) return lowValueComparison;
        var highValue = HiValue.CompareTo(other.HiValue);
        return highValue >= 0 ? 0 : highValue;
    }
}