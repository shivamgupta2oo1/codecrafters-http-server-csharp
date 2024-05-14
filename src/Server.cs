using System.Net;
using System.Net.Sockets;
// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");
// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
server.AcceptSocket(); // wait for client

using TcpClient client = await server.AcceptTcpClientAsync();
await using NetworkStream stream = client.GetStream();
// Read incoming data
byte[] requestBuffer = new byte[1024];
int bytesRead = await stream.ReadAsync(requestBuffer, 0, requestBuffer.Length);
string request =
    System.Text.Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);
Console.WriteLine($"Received request: {request}");
// Prepare and send response
string response = "HTTP/1.1 200 OK\r\n\r\n";
byte[] responseBuffer = System.Text.Encoding.UTF8.GetBytes(response);
stream.Write(responseBuffer, 0, responseBuffer.Length);