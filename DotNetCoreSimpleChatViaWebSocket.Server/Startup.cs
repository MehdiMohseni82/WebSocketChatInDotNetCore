using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetCoreSimpleChatViaWebSocket.Server
{
    public class Startup
    {
        private Dictionary<Guid, WebSocket> _webSocket = null;

        public Startup(IConfiguration configuration)
        {
            _webSocket = new Dictionary<Guid, WebSocket>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString("/chat")))
                {
                    var clientId = Guid.Parse(context.Request.Path.Value.Substring(6));

                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        _webSocket.Add(clientId, webSocket);
                        await Listen(context, webSocket, clientId);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
        }

        private async Task Listen(HttpContext context, WebSocket webSocket, Guid clientId)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                var str = System.Text.Encoding.Default.GetString(buffer);
                str = $"Client {clientId}: {str}";

                var msgBufferToSend = System.Text.Encoding.UTF8.GetBytes(str);
                await Task.Run(() =>
                {
                    foreach (var ws in _webSocket)
                    {
                        ws.Value.SendAsync(new ArraySegment<byte>(msgBufferToSend, 0, msgBufferToSend.Length), result.MessageType,
                            result.EndOfMessage, CancellationToken.None);
                    }
                });

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}