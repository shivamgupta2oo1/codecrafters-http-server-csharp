using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

class Program {
    static void Main(string[] args) {
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
            if (path == "/user-agent") {
                // Extract User-Agent header
                string userAgent = ExtractUserAgent(request);
                if (!string.IsNullOrEmpty(userAgent)) {
                    response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
                } else {
                    response = "HTTP/1.1 400 Bad Request\r\n\r\n";
                }
            } else if (path.StartsWith("/echo/")) {
                // Extract echo message from path
                string echoMessage = path.Substring("/echo/".Length);
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoMessage.Length}\r\n\r\n{echoMessage}";
            } else if (path == "/") {
                // Respond with 200 for root endpoint
                response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 0\r\n\r\n";
            } else {
                // Respond with 404 for other paths
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }
            // Send the response
            byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }

    static string ExtractPath(string request) {
        string[] lines = request.Split("\r\n");
        string[] parts = lines[0].Split(" ");
        return parts.Length > 1 ? parts[1] : "";
    }

    static string ExtractUserAgent(string request) {
        string[] lines = request.Split("\r\n");
        foreach (string line in lines) {
            if (line.StartsWith("User-Agent:")) {
                return line.Substring("User-Agent:".Length).Trim();
            }
        }
        return null;
    }
}
