using System.Net;
using System.Text;

namespace KayakDotNetChristmasChallenge2022;

public class HttpServer
{
    private const string FileName = "data/ips.db";
    public void Start()
    {
        HttpApi.RefreshIpList(FileName);
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8001/");
        listener.Start();
        Console.WriteLine("Listening on http://localhost:8001");
        Console.WriteLine("Try http://localhost:8001/refresh?IP2LOCATION-LITE-DB5.CSV");
        Console.WriteLine("Try http://localhost:8001/ips?value=123.123.132.123,123.123.132.124");
        while (true)
        {
            // Note: The GetContext method blocks while waiting for a request.
            HttpListenerContext context = listener.GetContext();

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            HandleRequest(request, response);
        }
    }

    private static void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        Console.WriteLine($"Received request for {request.Url}");
        var words = request.RawUrl.Split('?');
        var verb = words[0];
        var param = words.Length > 1 ? words[1] : String.Empty;

        var responseString = BuildResponseString(verb, param);
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    private static string BuildResponseString(string verb, string param)
    {
        return verb switch
        {
            "/ips" => HttpApi.BuildIpsResponse(param),
            "/refresh" => HttpApi.RefreshIpList(param),
            _ => "Wrong request"
        };
    }
}