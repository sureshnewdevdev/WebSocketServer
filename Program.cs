using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;// Classes for Websocket progtamming
using System.Text;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Enable WebSocket support
app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();// Establishing the connection
        await Echo(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

async Task Echo(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];

    try
    {
        // Receive the first message from the client
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        // Loop while the WebSocket is not closed
        while (result.MessageType != WebSocketMessageType.Close)
        {
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received: {receivedMessage}");


            // Check if the client has sent the message "close"
            //if (receivedMessage.Equals("close", StringComparison.OrdinalIgnoreCase))
            //{
            //    // Close the WebSocket connection with a normal closure status.
            //    await webSocket.CloseAsync(
            //        WebSocketCloseStatus.NormalClosure, // Indicate that the closure is normal.
            //        "Client requested closure", // Provide a reason for the closure.
            //        CancellationToken.None // Pass a cancellation token to allow cancellation of the operation.
            //    );
            //    Console.WriteLine("Client requested to close the WebSocket connection.");
            //    break; // Exit the loop after initiating the closure.
            //}

            // Prepare the server's reply message
            var serverReply = Encoding.UTF8.GetBytes($"Server says: {receivedMessage}");
            await webSocket.SendAsync(new ArraySegment<byte>(serverReply, 0, serverReply.Length), WebSocketMessageType.Text, true, CancellationToken.None);

            // Wait for the next message
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        // Handle closing the WebSocket properly
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        Console.WriteLine("WebSocket connection closed.");
    }
    catch (WebSocketException ex)
    {
        Console.WriteLine($"WebSocket error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
    finally
    {
        // Ensure WebSocket closure
        if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed connection", CancellationToken.None);
            Console.WriteLine("WebSocket connection forcefully closed.");
        }
    }
}

app.Run();
