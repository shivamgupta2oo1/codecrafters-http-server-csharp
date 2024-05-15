using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
static async Task Main(string[] args)
{
    if (args.Length != 2 || args[0] != "--directory")
    {
        Console.WriteLine("Usage: dotnet run -- --directory <directory>");
        return;
    }

    string directory = args[1];

    if (!Directory.Exists(directory))
    {
        Console.WriteLine("Directory not found.");
        return;
    }

    TcpListener server = new TcpListener(IPAddress.Any, 4221);
    server.Start();
    Console.WriteLine("Server started. Waiting for connections...");

    while (true)
    {
        TcpClient client = await server.AcceptTcpClientAsync();
        Console.WriteLine("Client connected.");
        _ = Task.Run(() => HandleClientAsync(client, directory));
    }
}

    static async Task HandleClientAsync(TcpClient client, string directory)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            string path = ExtractPath(request);
            string response;

            if (path.StartsWith("/files/"))
            {
                string filename = path.Substring("/files/".Length);
                string filePath = Path.Combine(directory, filename);

                if (File.Exists(filePath))
                {
                    string fileContents = File.ReadAllText(filePath);
                    response = GenerateResponse("200 OK", "application/octet-stream", fileContents);
                }
                else
                {
                    response = GenerateResponse("404 Not Found", "text/plain", "File Not Found");
                }
            }
            else
            {
                response = GenerateResponse("404 Not Found", "text/plain", "Not Found");
            }

            byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    static string ExtractPath(string request)
    {
        string[] lines = request.Split("\r\n");
        string[] parts = lines[0].Split(" ");
        return parts.Length > 1 ? parts[1] : "";
    }

    static string GenerateResponse(string status, string contentType, string responseBody)
    {
        string response = $"HTTP/1.1 {status}\r\n";
        response += $"Content-Type: {contentType}\r\n";
        response += $"Content-Length: {responseBody.Length}\r\n";
        response += "\r\n";
        response += responseBody;
        return response;
    }
}
