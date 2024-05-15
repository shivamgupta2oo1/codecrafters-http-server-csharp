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

        // Create the directory if it doesn't exist
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Console.WriteLine($"Directory '{directory}' created.");
        }

        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
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

                if (path.Equals("/"))
                {
                    response = GenerateResponse("200 OK", "text/plain", "Nothing");
                }
                else if (path.StartsWith("/files/"))
                {
                    string filename = path.Substring("/files/".Length);
                    string filePath = Path.Combine(directory, filename);

                    if (File.Exists(filePath))
                    {
                        string fileContents = ReadFile(filePath);
                        response = GenerateResponse("200 OK", "application/octet-stream", fileContents);
                    }
                    else
                    {
                        response = GenerateResponse("404 Not Found", "text/plain", "File Not Found");
                    }
                }
                else if (path.Equals("/user-agent"))
                {
                    string userAgent = GetUserAgent(request);
                    response = GenerateResponse("200 OK", "text/plain", userAgent);
                }
                else if (path.StartsWith("/echo"))
                {
                    string word = path.Split("/")[2];
                    response = GenerateResponse("200 OK", "text/plain", word);
                }
                else
                {
                    response = GenerateResponse("404 Not Found", "text/plain", "Nothing Dipshit");
                }

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
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

    static string ReadFile(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    static string GetUserAgent(string request)
    {
        string[] lines = request.Split("\r\n");
        return lines[2].Split(" ")[1];
    }

    static string GenerateResponse(string status, string contentType, string responseBody)
    {
        // Status Line
        string response = $"HTTP/1.1 {status}\r\n";

        // Headers
        response += $"Content-Type: {contentType}\r\n";
        response += $"Content-Length: {responseBody.Length}\r\n";
        response += "\r\n";

        // Response Body
        response += responseBody;

        return response;
    }
}
