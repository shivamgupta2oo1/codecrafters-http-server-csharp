using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

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
                    // Handle root action
                    case "":
                        status = RESP_200 + "\r\n";
                        break;

                    // Handle requests for files
                    case "files":
                        if (args.Length < 2)
                        {
                            status = RESP_404;
                            break;
                        }
                        string directoryName = null;
                        for (int i = 0; i < args.Length; i++)
                        {
                            if (args[i] == "--directory" && i + 1 < args.Length)
                            {
                                directoryName = args[i + 1];
                                break;
                            }
                        }
                        if (directoryName == null)
                        {
                            status = RESP_404;
                            break;
                        }
                        string fileName = Path.Combine(directoryName, requestParts.Length >= 3 ? requestParts[2] : "");

                        // Log the constructed file path
                        Console.WriteLine($"File path: {fileName}");

                        if (request.StartsWith("POST")) // Check if it's a POST request
                        {
                            // Extract file contents from the request body
                            int contentStartIndex = request.IndexOf("\r\n\r\n") + 4; // Find the start of the content
                            string fileContent = request.Substring(contentStartIndex);

                            // Save file contents to the specified directory
                            File.WriteAllText(fileName, fileContent);

                            // Send 201 response for successful file upload
                            status = RESP_201;
                        }
                        else if (File.Exists(fileName))
                        {
                            string fileContent = File.ReadAllText(fileName);
                            status = RESP_200 + "Content-Type: application/octet-stream\r\n";
                            status += $"Content-Length: {fileContent.Length}\r\n\r\n{fileContent}";
                        }
                        else
                        {
                            status = RESP_404;
                        }
                        break;

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
                        bool gzipAccepted = acceptEncoding.ToLower().Split(',').Any(e => e.Trim() == "gzip");
                        if (gzipAccepted)
                        {
                            responseHeaders += "Content-Encoding: gzip\r\n";
                        }
                        responseHeaders += "\r\n";
                        status = responseHeaders + echoMessage;

                        // Print out response headers for debugging
                        Console.WriteLine($"Response headers: {responseHeaders}");
                        break;

                    // Handle requests for user agent
                    case "user-agent":
                        string userAgent = "";
                        foreach (string rData in requestData)
                        {
                            if (rData.Contains("User-Agent"))
                            {
                                userAgent = rData.Split(" ")[1];
                                break;
                            }
                        }
                        status = RESP_200 + $"Content-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
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
}
