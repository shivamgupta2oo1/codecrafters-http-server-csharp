using System;
using System.IO;
using System.Linq;
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
            Console.WriteLine("Usage: ./your_server.sh --directory <directory>");
            return;
        }

        string directory = args[1];
        EnsureDirectoryExists(directory); // Ensure the directory exists
        GenerateSampleFile(directory);    // Generate a sample file in the directory

        using var server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Listening for connections...");

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            Task.Run(() => HandleClient(client, directory));
        }
    }

    static async Task HandleClient(TcpClient client, string directory)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            var response = HandleRequest(buffer, directory);
            var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true);
            writer.Write($"HTTP/1.1 {response.Message}\r\n");
            writer.Write($"Content-Type: {response.ContentType}\r\n");
            writer.Write($"Content-Length: {response.ContentLength}\r\n");
            writer.Write("\r\n");
            await writer.FlushAsync();
            await stream.WriteAsync(response.Content, 0, response.Content.Length);
        }
        finally
        {
            client.Close();
        }
    }

    static Response HandleRequest(byte[] requestBuffer, string directory)
    {
        var req = Encoding.UTF8.GetString(requestBuffer).Split("\r\n");
        var path = req[0].Split()[1];

        if (path == "/")
        {
            return new Response { Status = 200 };
        }
        else if (path.StartsWith("/files/"))
        {
            var content = path.Substring("/files/".Length);
            if (string.IsNullOrEmpty(content))
                return new Response { Status = 400 };

            var filePath = Path.Combine(directory, content);
            if (!File.Exists(filePath))
            {
                return new Response { Status = 404 };
            }

            var fileContent = File.ReadAllBytes(filePath);

            return new Response
            {
                Status = 200,
                Content = fileContent,
                ContentType = "application/octet-stream"
            };
        }
        else
        {
            return new Response { Status = 404 };
        }
    }

    static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Console.WriteLine($"Directory '{directory}' created.");
        }
    }

  static void GenerateSampleFile(string directory)
{
    string sampleDirectory = Path.Combine(directory, "autogenerated");
    Directory.CreateDirectory(sampleDirectory); // Ensure autogenerated directory exists
    string sampleFilePath = Path.Combine(sampleDirectory, "sample.txt");
    string sampleContent = "This is a sample file generated automatically.";
    File.WriteAllText(sampleFilePath, sampleContent);
    Console.WriteLine($"Sample file '{sampleFilePath}' generated.");
}

}

internal class Response
{
    public int Status { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "text/plain";

    public int ContentLength => Content.Length;
    public string Message => $"{Status} {Status switch
    {
        200 => "OK",
        404 => "Not Found",
        400 => "Wrong input"
    }}";
}
