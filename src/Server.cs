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
                // For the root URL ("/"), respond with a status code of 200
                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
            }
            else
            {
                // For other URLs, respond with a status code of 404
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusDescription = "Not Found";
            }

            // Set the status code before writing the response
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";

            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            Console.WriteLine("Response sent.");
        }
    }

    static string GetRequestedString(string[] segments)
    {
        if (segments.Length >= 2 && segments[1] != "/")
        {
            return segments[1].Trim('/');
        }
        else
        {
            // If the URL doesn't have enough segments, return an empty string
            return string.Empty;
        }
    }
}
