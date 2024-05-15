using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main(string[] args)
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
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connection Established");

                // Handle each client request in a separate thread
                _ = HandleClientAsync(client, directory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async System.Threading.Tasks.Task HandleClientAsync(TcpClient client, string directory)
    {
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received request: {request}");

                string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string path = lines[0].Split(' ')[1]; // Extracting the path from the HTTP request

                // Logic to handle different paths
                if (path.Equals("/"))
                {
                    byte[] response = generateResponse("200 OK", "text/plain", "Nothing");
                    await stream.WriteAsync(response, 0, response.Length);
                }
                else if (path.StartsWith("/files/"))
                {
                    string filename = path.Substring("/files/".Length);
                    string filePath = Path.Combine(directory, filename);

                    if (File.Exists(filePath))
                    {
                        string fileContents = File.ReadAllText(filePath);
                        byte[] response = generateResponse("200 OK", "text/plain", fileContents);
                        await stream.WriteAsync(response, 0, response.Length);
                    }
                    else
                    {
                        byte[] response = generateResponse("404 Not Found", "text/plain", "File Not Found");
                        await stream.WriteAsync(response, 0, response.Length);
                    }
                }
                else
                {
                    byte[] response = generateResponse("404 Not Found", "text/plain", "Resource Not Found");
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client request: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    static byte[] generateResponse(string status, string contentType, string responseBody)
    {
        // Status Line
        string response = $"HTTP/1.1 {status}\r\n";

        // Headers
        response += $"Content-Type: {contentType}\r\n";
        response += $"Content-Length: {responseBody.Length}\r\n";
        response += "\r\n";

        // Response Body
        response += responseBody;

        return Encoding.UTF8.GetBytes(response);
    }
}
