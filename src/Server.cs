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
        byte[] data = new byte[256];
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
                string[] requestArr = requestData[0].Split(" ");
                string requestType = requestArr[0];
                string action = requestURL.Split("/")[1];
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
                        string directoryName = args[0];
                        string fileName = Path.Combine(directoryName, requestURL.Split("/")[2]);

                        // Log the constructed file path
                        Console.WriteLine($"File path: {fileName}");

                        if (requestType == "GET")
                        {
                            // Handle GET requests for files
                            if (!File.Exists(fileName))
                            {
                                status = RESP_404;
                            }
                            else
                            {
                                string fileContent = File.ReadAllText(fileName);
                                status = RESP_200 + "Content-Type: text/plain\r\n";
                                status += $"Content-Length: {fileContent.Length}\r\n\r\n{fileContent}";
                            }
                        }
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
