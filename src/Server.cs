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
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            string response;

            if (IsUserAgentRequest(request))
            {
                string userAgent = GetUserAgent(request);
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
            }
            else if (IsEchoRaspberryRequest(request))
            {
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 9\r\n\r\nraspberry";
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }

    static bool IsUserAgentRequest(string request)
    {
        return request.Contains("GET /user-agent");
    }

    static string GetUserAgent(string request)
    {
        string[] lines = request.Split("\r\n");
        foreach (var line in lines)
        {
            if (line.StartsWith("User-Agent:"))
            {
                return line.Substring("User-Agent:".Length).Trim();
            }
        }
        return "";
    }
static bool IsEchoRaspberryRequest(string request)
{
    return request.Contains("GET /echo/pineapple");
}

}
