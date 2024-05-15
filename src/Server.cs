using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program {
    static void Main(string[] args) {
        // Check if the correct number of command-line arguments are provided
        if (args.Length != 2 || args[0] != "--directory") {
            Console.WriteLine("Usage: ./your_server.sh --directory <directory>");
            return;
        }

        // Extract the directory path from command-line arguments
        string directory = args[1];

        // Check if the directory exists
        if (!Directory.Exists(directory)) {
            Console.WriteLine("Directory not found.");
            return;
        }

        // Start TCP server
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true) {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client connected.");
            NetworkStream stream = client.GetStream();
            
            // Read the request
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            
            // Extract the path from the request
            string path = ExtractPath(request);
            
            // Prepare the response
            string response;
            if (path.StartsWith("/files/")) {
                // Extract filename
                string filename = path.Substring("/files/".Length);
                string filePath = Path.Combine(directory, filename);
                
                // Check if the file exists
                if (File.Exists(filePath)) {
                    // Read file contents
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    
                    // Prepare response with file contents
                    response = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileBytes.Length}\r\n\r\n";
                    byte[] responseHeaderBytes = Encoding.ASCII.GetBytes(response);
                    
                    // Send the response header
                    stream.Write(responseHeaderBytes, 0, responseHeaderBytes.Length);
                    
                    // Send the file contents
                    stream.Write(fileBytes, 0, fileBytes.Length);
                } else {
                    // File not found
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                    byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
            } else {
                // Respond with 404 for other paths
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
            
            // Close the stream and client connection
            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }

    // Helper method to extract path from the request
    static string ExtractPath(string request) {
        string[] lines = request.Split("\r\n");
        string[] parts = lines[0].Split(" ");
        return parts.Length > 1 ? parts[1] : "";
    }
}
