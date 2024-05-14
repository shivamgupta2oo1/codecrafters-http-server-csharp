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
            var response = context.Response;

            // Extract the requested string from the URL
            string requestedString = GetRequestedString(request.Url.Segments);

            Console.WriteLine($"Received request: {request.Url}");

            // Prepare response with the requested string
            string responseString = requestedString; // Set the response string dynamically
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "text/plain";

            // Check if the requested resource exists
            if (string.IsNullOrEmpty(requestedString))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusDescription = "Not Found";
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
            }

            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            Console.WriteLine("Response sent.");
        }
    }

    static string GetRequestedString(string[] segments)
    {
        if (segments.Length >= 3)
        {
            return segments[2];
        }
        else
        {
            // If the URL doesn't have enough segments, return an empty string
            return string.Empty;
        }
    }
}

