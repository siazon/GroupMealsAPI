using App.Infrastructure.Utility.Common;
using KingfoodIO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Use the Startup class
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
app.UseWebSockets();

app.Map("/ws", async context =>
{

    var webSocketManager = context.RequestServices.GetRequiredService<GMWebSocketManager>();

    if (context.WebSockets.IsWebSocketRequest)
    {
        var curName = context.Request.Query["name"];
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var clientId = Guid.NewGuid().ToString();

        webSocketManager.AddWebSocket(clientId, webSocket);
        Console.WriteLine($"WebSocket connected: {clientId}");

        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue)
            {
                await webSocketManager.WebSocketDisconnected(webSocket);
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await webSocketManager.ReceiveMessageAsync(webSocket, message);
            Console.WriteLine($"Received: {message}");
        }

        await webSocketManager.RemoveWebSocketAsync(clientId);

    }

});

var env = app.Services.GetRequiredService<IWebHostEnvironment>();
startup.Configure(app, env);

app.Run();
