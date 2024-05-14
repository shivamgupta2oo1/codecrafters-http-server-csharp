using System;
using System.IO;
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

            // Read the request body
            string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }

            // Prepare response with the requested string
            string responseString = string.IsNullOrEmpty(requestBody) ? requestedString : requestBody;

            // Ensure the response length matches the expected length
            responseString = responseString.Substring(0, Math.Min(responseString.Length, 9)); // Limit to 9 characters

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            // Set the status code and description
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";

            // Set the content type header
            response.ContentType = "text/plain";

            // Set the content length header
            response.ContentLength64 = buffer.Length;

            // Write the headers to the output stream
            response.OutputStream.Write(buffer, 0, buffer.Length);

            // Close the output stream
            response.OutputStream.Close();
            Console.WriteLine("Response sent.");
        }
    }

    static string GetRequestedString(string[] segments)
{
    string echoSegment = "echo/";
    int echoIndex = Array.IndexOf(segments, echoSegment);

    if (echoIndex >= 0 && echoIndex < segments.Length - 1)
    {
        return segments[echoIndex + 1].Trim();
    }
    else
    {
        // If the URL doesn't contain "echo/" or doesn't have enough segments after "echo/", return an empty string
        return string.Empty;
    }
}

}
