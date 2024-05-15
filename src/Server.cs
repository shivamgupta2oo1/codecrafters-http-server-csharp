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
            using (NetworkStream stream = client.GetStream())
            {
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
                        var responseObj = HandleRequest(filePath);
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write($"HTTP/1.1 {responseObj.Message}\r\n");
                            writer.Write($"Content-Type: {responseObj.ContentType}\r\n");
                            writer.Write($"Content-Length: {responseObj.ContentLength}\r\n");
                            writer.Write("\r\n");
                            await writer.WriteAsync(Encoding.UTF8.GetString(responseObj.Content));
                            await writer.FlushAsync();
                        }
                    }
                    else
                    {
                        response = "HTTP/1.1 404 Not Found\r\n\r\n";
                        byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                else
                {
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                    byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
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

    static Response HandleRequest(string filePath)
    {
        try
        {
            var buffer = File.ReadAllBytes(filePath);

            return new Response
            {
                Status = 200,
                Content = buffer,
                ContentType = "application/octet-stream"
            };
        }
        catch (Exception)
        {
            return new Response { Status = 500 }; // Internal Server Error
        }
    }
}

class Response
{
    public byte[] Content { get; set; } = new byte[] { };
    public string ContentType { get; set; } = "text/plain";
    public int ContentLength => Content.Length;
    public string Message => $"{Status} {Status switch { 200 => "OK", 404 => "Not Found", 500 => "Internal Server Error" }}";
    public int Status { get; set; }
}
