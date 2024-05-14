using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            string[] lines = request.Split("\r\n");
            string[] requestLine = lines[0].Split(' ');
            string path = requestLine[1];

            string response;

            if (path.StartsWith("/echo/"))
            {
                // Extract the string from the URL path
                string str = path.Substring(6); // Remove "/echo/"
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {str.Length}\r\n\r\n{str}";
            }
            else if (path == "/")
            {
                response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 3\r\n\r\nabc";
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }
}
