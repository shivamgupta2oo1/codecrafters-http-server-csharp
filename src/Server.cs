using System;
using System.Net;

class Program
{
    static void Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:4221/");
        listener.Start();
        Console.WriteLine("Server started. Listening for connections...");

        while (true)
        {
            var context = listener.GetContext();
            var request = context.Request;
            Console.WriteLine(request);
            var response = context.Response;

            // Extract the requested string from the URL
            string requestedString = request.Url.Segments.Length >= 2 ? request.Url.Segments[2] : string.Empty;

            Console.WriteLine($"Received request: {request.Url}");

            // Prepare response with the requested string
            string responseString = requestedString;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;

            // Send the response
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            Console.WriteLine("Response sent.");
        }
    }
}
