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
            // **Ensure server starts before tests**
            TcpListener server = new TcpListener(IPAddress.Any, 4221);
            server.Start(); // Start the server listening on port 4221
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
                // ... (rest of the code for handling client requests)
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
