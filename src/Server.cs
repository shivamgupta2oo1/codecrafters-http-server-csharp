using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        // Print statements for debugging
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
                        string echoData = requestParts.Length >= 3 ? requestParts[2] : "";
                        string acceptEncoding = "";
                        foreach (string rData in requestData)
                        {
                            if (rData.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase))
                            {
                                acceptEncoding = rData.Split(":")[1].Trim().ToLower();
                                break;
                            }
                        }
                        bool gzipRequested = acceptEncoding.Contains("gzip");
                        StringBuilder responseBuilder = new StringBuilder();
                        responseBuilder.Append(RESP_200);
                        if (gzipRequested)
                        {
                            // Compress the response body using gzip encoding
                            byte[] gzipData = CompressStringToGZip(echoData);
                            int gzipDataLength = gzipData.Length;
                            responseBuilder.Append("Content-Encoding: gzip\r\n");
                            responseBuilder.Append($"Content-Length: {gzipDataLength}\r\n\r\n");
                            byte[] headerBytes = Encoding.ASCII.GetBytes(responseBuilder.ToString());
                            stream.Write(headerBytes, 0, headerBytes.Length);
                            stream.Write(gzipData, 0, gzipData.Length);
                        }
                        else
                        {
                            // If gzip encoding is not requested, send the response as plain text
                            responseBuilder.Append("Content-Type: text/plain\r\n");
                            responseBuilder.Append($"Content-Length: {echoData.Length}\r\n\r\n{echoData}");
                            status = responseBuilder.ToString();
                            byte[] response = Encoding.ASCII.GetBytes(status);
                            stream.Write(response, 0, response.Length);
                        }
                        break;
                    // Handle unknown actions
                    default:
                        status = RESP_404;
                        byte[] response404 = Encoding.ASCII.GetBytes(status);
                        stream.Write(response404, 0, response404.Length);
                        break;
                }
            }
        }
    }

    // Compress a string using GZip
    private static byte[] CompressStringToGZip(string text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(buffer, 0, buffer.Length);
            }
            return memoryStream.ToArray();
        }
    }
}
