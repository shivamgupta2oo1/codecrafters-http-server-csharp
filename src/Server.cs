using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
        if (IsEchoRequest(request, out string echoValue))
        {
            return $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoValue.Length}\r\n\r\n{echoValue}";
        }
        else if (IsUserAgentRequest(request, out string userAgent))
        {
            return $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
        }
        else
        {
            return "HTTP/1.1 404 Not Found\r\n\r\n";
        }
    }

    static bool IsEchoRequest(string request, out string value)
    {
        value = "";
        if (request.StartsWith("GET /echo/"))
        {
            value = request.Substring(10).Trim();
            return true;
        }
        return false;
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
}
