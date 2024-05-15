using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            using (TcpClient client = server.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                string response = HandleRequest(request);

                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);

                Console.WriteLine("Response sent. Client disconnected.");
            }
        }
    }

    static string HandleRequest(string request)
    {
        if (IsUserAgentRequest(request, out string userAgent))
        {
            return GenerateResponse(200, $"User-Agent: {userAgent}");
        }
        else if (IsEchoRequest(request, out string echoValue))
        {
            return GenerateResponse(200, echoValue);
        }
        else
        {
            return GenerateResponse(404, "");
        }
    }

    static bool IsUserAgentRequest(string request, out string userAgent)
    {
        userAgent = "";
        if (request.StartsWith("GET /user-agent"))
        {
            foreach (var line in request.Split('\n'))
            {
                if (line.StartsWith("User-Agent:"))
                {
                    userAgent = line.Substring("User-Agent:".Length).Trim();
                    return true;
                }
            }
        }
        return false;
    }

    static bool IsEchoRequest(string request, out string value)
    {
        value = "";
        Match match = Regex.Match(request, @"GET /echo/(.+)");
        if (match.Success)
        {
            value = match.Groups[1].Value.Trim();
            return true;
        }
        return false;
    }

static string GenerateResponse(int statusCode, string body)
{
    string statusLine = $"HTTP/1.1 {statusCode} {GetStatusMessage(statusCode)}\r\n";
    string responseBody = body + "\r\n"; // Add CRLF at the end of the body
    int contentLength = Encoding.ASCII.GetByteCount(responseBody) - 2; // Subtract 2 for the added CRLF
    string headers = $"Content-Type: text/plain\r\nContent-Length: {contentLength}\r\n\r\n";
    return statusLine + headers + responseBody;
}



    static string GetStatusMessage(int statusCode)
    {
        switch (statusCode)
        {
            case 200: return "OK";
            case 404: return "Not Found";
            default: return "Unknown";
        }
    }
}
