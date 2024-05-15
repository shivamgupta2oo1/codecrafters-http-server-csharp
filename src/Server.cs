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
        string RESP_201 = "HTTP/1.1 201 Created\r\n\r\n";
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
                    // Handle POST requests for files
                    case "files":
                        string directoryName = args.Length >= 2 ? args[1] : "";
                        string fileName = Path.Combine(directoryName, requestParts[2]);
                        string content = requestData[requestData.Length - 1];
                        int contentLength = 0;
                        foreach (string rData in requestData)
                        {
                            if (rData.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            {
                                contentLength = int.Parse(rData.Split(" ")[1]);
                                break;
                            }
                        }
                        File.WriteAllText(fileName, content.Substring(0, contentLength));
                        status = RESP_201;
                        byte[] response201 = Encoding.ASCII.GetBytes(status);
                        stream.Write(response201, 0, response201.Length);
                        break;
                    // Handle GET requests for gzip-compressed echo
                    case "echo":
                        string echoData = requestParts[2];
                        string contentEncoding = "";
                        foreach (string rData in requestData)
                        {
                            if (rData.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase))
                            {
                                // Extract the value of the Accept-Encoding header
                                contentEncoding = rData.Split(":")[1].Trim().ToLower();
                                break;
                            }
                        }
                        // Check if gzip compression is requested
                        bool gzipRequested = contentEncoding.Contains("gzip");
                        // Prepare the response with appropriate headers
                        StringBuilder responseBuilder = new StringBuilder();
                        responseBuilder.Append(RESP_200);
                        if (gzipRequested)
                        {
                            responseBuilder.Append("Content-Encoding: gzip\r\n");
                            // Compress the response body using gzip encoding
                            using (MemoryStream ms = new MemoryStream())
                            {
                                using (GZipStream gzip =
                                    new GZipStream(ms, CompressionMode.Compress, true))
                                {
                                    byte[] bytes = Encoding.UTF8.GetBytes(echoData);
                                    gzip.Write(bytes, 0, bytes.Length);
                                }
                                // Get the gzip compressed data
                                byte[] gzipData = ms.ToArray();
                                // Calculate the length of the gzip-encoded data
                                int gzipDataLength = gzipData.Length;
                                // Add headers for gzip encoding and content length
                                responseBuilder.Append($"Content-Length: {gzipDataLength}\r\n\r\n");
                                byte[] headerBytes =
                                    Encoding.ASCII.GetBytes(responseBuilder.ToString());
                                stream.Write(headerBytes, 0, headerBytes.Length);
                                stream.Write(gzipData, 0, gzipData.Length);
                            }
                        }
                        else
                        {
                            // If gzip encoding is not requested, send the response as plain text
                            responseBuilder.Append(
                                $"Content-Length: {echoData.Length}\r\n\r\n{echoData}");
                            status = responseBuilder.ToString();
                            byte[] response = Encoding.ASCII.GetBytes(status);
                            stream.Write(response, 0, response.Length);
                        }
                        break;
                    // Handle other actions (e.g., echo)
                    default:
                        status = RESP_404;
                        byte[] response404 = Encoding.ASCII.GetBytes(status);
                        stream.Write(response404, 0, response404.Length);
                        break;
                }
            }
        }
    }
}
