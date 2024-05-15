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
                    var responseObj = HandleRequest(buffer, args);
                    var writer = new StreamWriter(stream);
                    writer.Write($"HTTP/1.1 {responseObj.Message}\r\n");
                    writer.Write($"Content-Type: {responseObj.ContentType}\r\n");
                    writer.Write($"Content-Length: {responseObj.ContentLength}\r\n");
                    writer.Write("\r\n");
                    writer.Write(Encoding.UTF8.GetString(responseObj.Content));
                    writer.Flush();
                    stream.Dispose();
                    client.Dispose();
                }
                else
                {
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                }
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
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

    static Response HandleRequest(byte[] requestBuffer, string[] args)
    {
        var req = Encoding.UTF8.GetString(requestBuffer).Split("\r\n");
        var path = req[0].Split(" ")[1];

        if (path.StartsWith("/echo/"))
        {
            path = path.Substring(1, path.Length - 1);
            var content = path.Split('/')[1];
            if (string.IsNullOrEmpty(content))
                return new Response { Status = 400 };
            return new Response { Status = 200, Content = Encoding.UTF8.GetBytes(content) };
        }
        else if (path.StartsWith("/files/"))
        {
            var content = path.Substring("/files/".Length);
            if (string.IsNullOrEmpty(content))
                return new Response { Status = 400 };
            var directory = args[1];
            string filePath = Path.Combine(directory, content);

            if (!File.Exists(filePath))
            {
                return new Response
                {
                    Status = 404
                };
            }

            var stream = File.OpenRead(filePath);
            var buffer = new byte[1024];
            var length = stream.Read(buffer);
            buffer = buffer.Take(length).ToArray();

            return new Response
            {
                Status = 200,
                Content = buffer.ToArray(),
                ContentType = "application/octet-stream"
            };
        }
        else if (path.StartsWith("/user-agent"))
        {
            var userAgent = req[2].Split(" ")[1];
            return new Response { Status = 200, Content = Encoding.UTF8.GetBytes(userAgent) };
        }

        return new Response { Status = 404 };
    }
}

class Response
{
    public byte[] Content { get; set; } = new byte[] { };
    public string ContentType { get; set; } = "text/plain";
    public int ContentLength => Content.Length;
    public string Message => $"{Status} {Status switch { 200 => "OK", 404 => "Not Found", 400 => "Wrong input" }}";
    public int Status { get; set; }
}
