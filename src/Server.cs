using System;
using System.Net;
using System.Net.Sockets;

class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Waiting for client...");
        
        using TcpClient client = await server.AcceptTcpClientAsync();
        Console.WriteLine("Client connected.");
        
        await using NetworkStream stream = client.GetStream();
        byte[] requestBuffer = new byte[1024];
        
        // Read incoming data
        int bytesRead = await stream.ReadAsync(requestBuffer, 0, requestBuffer.Length);
        
        if (bytesRead == 0)
        {
            Console.WriteLine("No data received.");
            return;
        }
        
        string request = System.Text.Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);
        Console.WriteLine($"Received request: {request}");
        
        // Check if it's a valid HTTP request
        if (!request.StartsWith("GET"))
        {
            Console.WriteLine("Invalid HTTP request.");
            return;
        }
        
        // Prepare and send response
        string response = "HTTP/1.1 200 OK\r\n\r\nHello, World!";
        byte[] responseBuffer = System.Text.Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
        Console.WriteLine("Response sent.");
    }
}
