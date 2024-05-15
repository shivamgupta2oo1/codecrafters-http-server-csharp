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

        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Connection Established");

            // Create response buffer and get response
            byte[] requestText = new byte[100];
            client.GetStream().Read(requestText, 0, requestText.Length);

            // Parse request path
            string parsed = Encoding.UTF8.GetString(requestText);
            Console.WriteLine($"Received request: {parsed}"); // Print out the received request for debugging
            string[] parsedLines = parsed.Split("\r\n");

            // Capturing specific parts of the request that will always be there
            string method = parsedLines[0].Split(" ")[0]; // GET, POST
            string path = parsedLines[0].Split(" ")[1]; // /echo/apple

            // Logic
            if (path.Equals("/"))
            {
                client.GetStream().Write(generateResponse("200 OK", "text/plain", "Nothing"));
            }

            // Return if file specified after '/files/' exists, return contents in response body
            else if (path.StartsWith("/files/"))
            {
                string filename = path.Substring("/files/".Length);
                string filePath = Path.Combine(directory, filename);

                if (File.Exists(filePath))
                {
                    string fileContents = readFile(filePath);
                    byte[] responseBytes = generateResponse("200 OK", "application/octet-stream", fileContents);
                    client.GetStream().Write(responseBytes);
                }
                else
                {
                    byte[] responseBytes = generateResponse("404 Not Found", "text/plain", "File Not Found");
                    client.GetStream().Write(responseBytes);
                }
            }

            // ... (rest of your logic for other paths)

            client.Close();
            Console.WriteLine("Client disconnected.");
        }

        server.Stop();
    }

    static byte[] generateResponse(string status, string contentType, string responseBody)
    {
        // Status Line
        string response = $"HTTP/1.1 {status}\r\n";

        // Headers
        response += $"Content-Type: {contentType}\r\n";
        response += $"Content-Length: {responseBody.Length}\r\n"; // This line uses responseBody.Length to get the actual byte length
        response += "\r\n";

        // Response Body
        response += responseBody;

        return Encoding.UTF8.GetBytes(response); // This line converts the string response to a byte array
    }

    static string readFile(string filepath)
    {
        try
        {
            // Read file using stream with better error handling
            using (StreamReader reader = new StreamReader(filepath))
            {
                return reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error reading file: {e.Message}");
            return "";
        }
    }
}
