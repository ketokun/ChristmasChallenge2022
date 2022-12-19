using System;
using System.Diagnostics;
using System.Threading.Tasks;
using KayakDotNetChristmasChallenge2022;
using NUnit.Framework;

namespace ApiTest;

[TestFixture]
public class HttpApiTest
{
    [Test]
    public async Task BuildIpResponseTest()
    {
        var fileName = "data/IP2LOCATION-LITE-DB5.CSV";
        var refreshResponse = HttpApi2.RefreshIpList(fileName);
        Console.WriteLine(refreshResponse);
        
        var response = HttpApi2.BuildIpsResponse("value=223.187.136.254,1.0.245.255");
        Console.WriteLine(response);
        
        Assert.That(response.StartsWith("India/Ramgarh,Thailand/Ko Samui"), Is.True);

    }
}