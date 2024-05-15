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
        // ... (rest of the code)

        try
        {
            // ... (server setup)

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

                // ... (process request and generate response)

                // **Ensure complete response is written**
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

    // ... (rest of the code for handling requests and responses)
}
