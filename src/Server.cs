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
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}");
                return;
            }
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

                if (!File.Exists(filePath))
                {
                    try
                    {
                        File.WriteAllText(filePath, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating file: {ex.Message}");
                        response = "HTTP/1.1 500 Internal Server Error\r\n\r\n";
                        byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        return;
                    }
                }

                byte[] fileBytes = File.ReadAllBytes(filePath);
                response = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileBytes.Length}\r\n\r\n";
                byte[] responseHeaderBytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(responseHeaderBytes, 0, responseHeaderBytes.Length);
                await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
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
}
