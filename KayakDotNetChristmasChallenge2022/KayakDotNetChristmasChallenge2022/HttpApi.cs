using System.Collections.Concurrent;
using System.Diagnostics;

namespace KayakDotNetChristmasChallenge2022;

public class HttpApi
{
    private static readonly Process _process = Process.GetCurrentProcess();
    private static IpLocationData[] _ipLocationDataEntries;
    private static IpWithIndex[] _index;
    private static IEnumerable<string> _allLines;
    private static HashSet<string> _country; //AUAustralia
    private static Dictionary<int, string> _state;
    private static Dictionary<int, string> _city;

    /// http://localhost:8001/refresh?../../IP2LOCATION-LITE-DB5.CSV
    public static string RefreshIpList(string fileName)
    {
        var sw = Stopwatch.StartNew();
        _index = GetLocationsFromFile(fileName);
        sw.Stop();
        var responseString = "GeoIp table loaded " + sw.Elapsed;

        var memoryAfterLoadInMb = _process.WorkingSet64 / (1024 * 1024);

        responseString += $"{Environment.NewLine}MemoryAfterLoad: {memoryAfterLoadInMb} MB";
        
        return responseString;
    }

    private static IpWithIndex[] GetLocationsFromFile(string fileName)
    {
        var array = new ConcurrentBag<IpWithIndex>();
        _allLines = File.ReadLines(fileName);
        Parallel.ForEach(_allLines, (line, _, index) => 
        {
            array.Add(IpLocationData.getLowIpWithIndex(line, (uint)index));
        });
        return array.OrderBy(x=>x.LowIp).ToArray();
    }

    /// handling calls like:
    /// http://localhost:8001/ips?value=123.123.132.123,123.123.132.124
    public static string BuildIpsResponse(string ipData, string fileName)
    {
        var sw = Stopwatch.StartNew();
        var ipValue = ipData.Split('=')[1];
        var ipDataArray = ipValue.Split(',');
        var ipItems = ipDataArray.Select(GetIpWithIndex);
        var result = new List<IpLocationData>();
        foreach (var ipItem in ipItems)
        {
            var searchedIndex = Array.BinarySearch(_index, ipItem);
            if (searchedIndex >= 0)
                result.Add(GetLocationByIndex(fileName, _index[searchedIndex].FileIndex));
            else
            {
                var index = ~searchedIndex;
                if (index <= _index.Length - 1 && index > 0)
                {
                    var location = GetLocationByIndex(fileName, _index[index - 1].FileIndex);
                    if (location.HiValue >= ipItem.LowIp)
                        result.Add(location);
                }
            }
        }

        sw.Stop();

        var response = String.Join(",", result.Select(ipDataItem => $"{ipDataItem.Country}/{ipDataItem.City}")) +
                       "\nTime to process " + sw.Elapsed;
        return response;
    }

    private static IpLocationData GetLocationByIndex(string fileName, uint fileIndex)
    {
        string line;
        using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var bs = new BufferedStream(fs))
        using (var sr = new StreamReader(bs))
        {
            var i = 0;
            while ( i ++ < fileIndex && (line = sr.ReadLine()) != null)
            {
            }

            line = sr.ReadLine();
        }

        //line = _allLines.Skip((int)fileIndex).Take(1).First();
        return IpLocationData.MapLocationFromStringRow(line);
    }

    private static IpWithIndex GetIpWithIndex(string ip)
    {
        var components = ip.Split('.');
        uint result = 0;
        for (var i = 0; i < 4; i++)
        {
            result = result * 256 + uint.Parse(components[i]);
        }

        return new IpWithIndex(result, 0);
    }
}