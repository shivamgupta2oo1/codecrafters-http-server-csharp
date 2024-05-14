using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Listening for connections...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            _ = Task.Run(async () =>
            {
                using NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Extract the request path
                string[] requestLines = request.Split('\n', '\r');
                string requestPath = requestLines[0].Split(' ')[1];

                Console.WriteLine($"Received request: {request}");

                // Prepare response based on requested path
                string response;
                if (requestPath == "/abcdefg")
                {
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                }
                else if (requestPath == "/")
                {
                    response = "HTTP/1.1 200 OK\r\n\r\n";
                }
                else
                {
                    // For any other path, respond with 404 Not Found
                    response = "HTTP/1.1 404 Not Found\r\n\r\n";
                }

                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

                Console.WriteLine("Response sent.");
                client.Close();
            });
        }
    }
}
