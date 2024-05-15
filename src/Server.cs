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
            Console.WriteLine("Usage: ./your_server.sh --directory <directory>");
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

        try
        {
            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");
                _ = Task.Run(() => HandleClientAsync(client, directory));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            server.Stop();
        }
    }

    static async Task HandleClientAsync(TcpClient client, string directory)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                // Read the request asynchronously
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                // Extract the path from the request
                string path = ExtractPath(request);
                // Prepare the response
                string response;
                if (path.StartsWith("/files/"))
                {
                    // Extract filename
                    string filename = path.Substring("/files/".Length);
                    string filePath = Path.Combine(directory, filename);
                    if (File.Exists(filePath))
                    {
                        // Read file contents
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        // Prepare response with file contents
                        response = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileBytes.Length}\r\n\r\n";
                        byte[] responseHeaderBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(responseHeaderBytes, 0, responseHeaderBytes.Length);
                        await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                    }
                    else
                    {
                        // File not found
                        response = "HTTP/1.1 404 Not Found\r\n\r\n";
                        byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                else
                {
                    // Respond with 404 for other paths
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                    byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while handling client request: {ex.Message}");
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
