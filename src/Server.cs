using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

byte[] generateResponse(string status, string contentType, string responseBody)
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

string readFile(string filepath)
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


// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Create TcpListener
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    // Create new socket
    Socket client = server.AcceptSocket();
    Console.WriteLine("Connection Established");

    // Create response buffer and get response
    byte[] requestText = new byte[100];
    client.Receive(requestText);

    // Parse request path
    string parsed = System.Text.Encoding.UTF8.GetString(requestText);
    string[] parsedLines = parsed.Split("\r\n");

    // Capturing specific parts of the request that will always be there
    string method = parsedLines[0].Split(" ")[0]; // GET, POST
    string path = parsedLines[0].Split(" ")[1]; // /echo/apple

    // Logic
    if (path.Equals("/"))
    {
        client.Send(generateResponse("200 OK", "text/plain", "Nothing"));
    }

    // Return if file specified after '/files/' exists, return contents in response body
    else if (path.StartsWith("/files/"))
    {
        // Instructions mention the program WILL be run like this ./program --directory dir
        string directoryName = args[1];
        string filename = path.Split("/")[2];

        string fileContents = readFile(Path.Combine(directoryName, filename));

        if (fileContents.Length > 0)
        {
            client.Send(generateResponse("200 OK", "application/octet-stream", fileContents));
        }
        else
        {
            client.Send(generateResponse("404 Not Found", "text/plain", "File Not Found"));
        }
    }

    // ... (rest of your logic for other paths)

    client.Close();
}

server.Stop();
