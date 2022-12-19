using System.Data.SQLite;
using System.Diagnostics;

namespace KayakDotNetChristmasChallenge2022;

public class HttpApi2
{
    private static readonly Process _process = Process.GetCurrentProcess();

    private static LocationData _locationData;

    /// http://localhost:8001/refresh?../../IP2LOCATION-LITE-DB5.CSV
    public static string RefreshIpList(string fileName)
    {
        var sw = Stopwatch.StartNew();

        _locationData = GetLocationsFromFile(fileName).Result;
        sw.Stop();
        var responseString = "GeoIp table loaded " + sw.Elapsed;

        var memoryAfterLoadInMb = _process.WorkingSet64 / (1024 * 1024);

        responseString += $"{Environment.NewLine}MemoryAfterLoad: {memoryAfterLoadInMb} MB";

        return responseString;
    }

    private struct LocationData
    {
        public Location[] Locations;
        public IpRange[] Ips;
    }

    private static async Task<Location[]> GetLocations()
    {
        var locations =  Array.Empty<Location>();
        using var fileDb = new SQLiteConnection($"Data Source=data/locations.db;FailIfMissing=True;");
        fileDb.Open();
        var command = fileDb.CreateCommand();
            
        command.CommandText = "SELECT count(*) from locations";
        await using (var rdr = await command.ExecuteReaderAsync())
        {
            await rdr.ReadAsync();
            locations = new Location[rdr.GetInt32(0)];
        }
            
        command.CommandText = "SELECT * from locations";
        await using (var rdr = await command.ExecuteReaderAsync())
        {
            var i = 0;
            while (await rdr.ReadAsync())
            { 
                locations[i] = new Location
                {
                    CountryCode = string.Intern(rdr.GetString(1)),
                    Country = string.Intern(rdr.GetString(2)),
                    Region = string.Intern(rdr.GetString(3)),
                    City = rdr.GetString(4),
                    Lat = rdr.GetDouble(5),
                    Lon = rdr.GetDouble(6)
                };
                i++;
            }
        }

        return locations;
    }
    
    private static async Task<IpRange[]> GetIpRanges()
    { 
        var ips = Array.Empty<IpRange>();
        using var fileDb = new SQLiteConnection($"Data Source=data/ipsPresorted.db;FailIfMissing=True;");
        fileDb.Open();
        var command = fileDb.CreateCommand();
            
        command.CommandText = "SELECT count(*) from ranges";
        await using (var rdr = await command.ExecuteReaderAsync())
        {
            await rdr.ReadAsync();
            ips = new IpRange[rdr.GetInt32(0)];
        }


        command.CommandText = "SELECT * from ranges";
        await using (var rdr =await command.ExecuteReaderAsync())
        {
            int i = 0;
            while (await rdr.ReadAsync())
            {
                ips[i] = new IpRange
                {
                    LowValue = (uint) rdr.GetInt64(0), HiValue = (uint) rdr.GetInt64(1),
                    LocationID = rdr.GetInt32(2)
                };
                i++;
            }
        }

        return ips;
    }
    
    private static async Task<LocationData> GetLocationsFromFile(string fileName)
    {
        // -----------------------------------------------------
        bool isGenerator = false;
        // -----------------------------------------------------
        
        if (!isGenerator)
        {
            var t1 = Task.Run(GetLocations);
            var t2 = Task.Run(GetIpRanges);
            var locations = await t1;
            var ips = await t2;
            GC.Collect();
            return new LocationData {Locations = locations.ToArray(), Ips = ips};
        }

        if (isGenerator)
        {
            var CsvData = File.ReadLines(fileName);

            SortAndSaveToSQLite(CsvData.Select(IpLocationData.ParserRow).ToList());
        }

        return new LocationData();
    }

    private static void SortAndSaveToSQLite(IList<(IpRange ipRange, Location location)> locationData)
    {
        var locationIds = new Dictionary<long, long>();
        string cs = "Data Source=:memory:";
        using var con1 = new SQLiteConnection(cs);
        con1.Open();

        using var cmd = new SQLiteCommand(con1);

        cmd.CommandText = "DROP TABLE IF EXISTS ranges";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS locations";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE locations(ID INTEGER PRIMARY KEY,
          CountryCode varchar(2), Country varchar(100), Region varchar(100), City varchar(100), Lat real, Lon real)";
        cmd.ExecuteNonQuery();

        foreach (var data in locationData.OrderBy(x => x.ipRange.LowValue).ToList())
        {
            long locationId = 0;
            bool hasLocation = false;

            if (!locationIds.TryGetValue(data.location.GetHashCode(), out locationId))
            {
                cmd.CommandText = $"INSERT INTO locations(CountryCode,Country,Region,City,Lat,Lon) " +
                                  $"VALUES('{data.location.CountryCode}'," +
                                  $"'{data.location.Country.Replace("'", "''")}'," +
                                  $"'{data.location.Region.Replace("'", "''")}'," +
                                  $"'{data.location.City.Replace("'", "''")}'," +
                                  $"'{data.location.Lat}'," +
                                  $"'{data.location.Lon}')";
                cmd.ExecuteNonQuery();
                locationId = con1.LastInsertRowId;
                locationIds.Add(data.location.GetHashCode(), locationId);
            }
        }
        
        cmd.CommandText = "VACUUM INTO 'file:locations.db'";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = "DROP TABLE IF EXISTS locations";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = @"CREATE TABLE ranges(LowValue INTEGER,
            HiValue INTEGER, LocationID integer)";
        cmd.ExecuteNonQuery();

        foreach (var data in locationData.OrderBy(x => x.ipRange.LowValue).ToList())
        {
            long locationId = 0;
            bool hasLocation = false;

            locationIds.TryGetValue(data.location.GetHashCode(), out locationId);
                
            cmd.CommandText =
                $"INSERT INTO ranges(LowValue, HiValue, LocationID) VALUES({data.ipRange.LowValue},{data.ipRange.HiValue},{locationId})";
            cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "VACUUM INTO 'file:ipsPresorted.db'";
        cmd.ExecuteNonQuery();
    }

    /// handling calls like:
    /// http://localhost:8001/ips?value=123.123.132.123,123.123.132.124
    public static string BuildIpsResponse(string ipData)
    {
        var sw = Stopwatch.StartNew();
        var ipValue = ipData.Split('=')[1];
        var ipDataArray = ipValue.Split(',');
        var ipItems = ipDataArray.Select(IpToUint);
        var result = new List<(IpRange ip, Location location)>();
        foreach (var ipItem in ipItems)
        {
            var searchedIndex = Array.BinarySearch(_locationData.Ips,
                new IpRange {LowValue = ipItem, HiValue = ipItem});
            var theIp = _locationData.Ips[searchedIndex];
            var location = _locationData.Locations[theIp.LocationID-1];
            if (searchedIndex >= 0)
                result.Add((theIp, location));
        }

        sw.Stop();

        var response = String.Join(",",
                           result.Select(ipDataItem => $"{ipDataItem.location.Country}/{ipDataItem.location.City}")) +
                       "\nTime to process " + sw.Elapsed;
        return response;
    }

    private static uint IpToUint(string ip)
    {
        var components = ip.Split('.');
        uint result = 0;
        for (var i = 0; i < 4; i++)
        {
            result = result * 256 + uint.Parse(components[i]);
        }

        return result;
    }
}