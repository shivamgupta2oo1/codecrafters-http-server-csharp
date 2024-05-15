using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        // When running tests.
        Console.WriteLine("Logs from your program will appear here!");

        // Start TCP server
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        byte[] data = new byte[1024]; // Increase buffer size for potential larger file uploads
        string RESP_200 = "HTTP/1.1 200 OK\r\n";
        string RESP_404 = "HTTP/1.1 404 Not Found\r\n\r\n";

        while (true)
        {
            Console.Write("Waiting for a connection... ");
            using (TcpClient client = server.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            {
                Console.WriteLine("Connected!");

                // Read request data
                int bytesRead = stream.Read(data, 0, data.Length);
                string request = Encoding.ASCII.GetString(data, 0, bytesRead);
                string[] requestData = request.Split("\r\n");
                string requestURL = requestData[0].Split(" ")[1];
                string[] requestParts = requestURL.Split("/");
                string action = requestParts.Length >= 2 ? requestParts[1] : "";
                string status = "";

                // Handle different actions based on request
                switch (action)
                {
                    // Handle echo requests
                    case "echo":
                        string echoMessage = requestParts.Length >= 3 ? requestParts[2] : "";
                        string acceptEncoding = "";
                        foreach (string rData in requestData)
                        {
                            if (rData.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase))
                            {
                                acceptEncoding = rData.Split(" ")[1];
                                break;
                            }
                        }
                        string responseHeaders = RESP_200 + $"Content-Type: text/plain\r\nContent-Length: {echoMessage.Length}\r\n";
                        bool gzipAccepted = acceptEncoding.ToLower().Contains("gzip");
                        if (gzipAccepted)
                        {
                            responseHeaders += "Content-Encoding: gzip\r\n";
                            echoMessage = CompressString(echoMessage);
                        }
                        responseHeaders += "\r\n";
                        status = responseHeaders + echoMessage;

                        // Print out response headers for debugging
                        Console.WriteLine($"Response headers: {responseHeaders}");
                        break;

                    // Handle unknown actions
                    default:
                        status = RESP_404;
                        break;
                }

                // Write response to the client
                byte[] response = Encoding.ASCII.GetBytes(status);
                stream.Write(response, 0, response.Length);
            }
        }
    }

    // Compress string using GZip
    private static string CompressString(string text)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (System.IO.Compression.GZipStream gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Compress))
            {
                using (StreamWriter writer = new StreamWriter(gzipStream))
                {
                    writer.Write(text);
                }
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
