using App.Domain.Common.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{

    public class GMWebSocketManager
    {
        private readonly Dictionary<string, WebSocket> _webSockets = new();

        public void AddWebSocket(string id, WebSocket socket)
        {
            //_webSockets[id] = socket;
        }

        public WebSocket? GetWebSocket(string id)
        {
            _webSockets.TryGetValue(id, out var socket);
            return socket;
        }

        public async Task SendMessageAsync(string id, string message)
        {
            if (_webSockets.TryGetValue(id, out var socket) && socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        public async Task ReceiveMessageAsync(WebSocket socket, string message)
        {
            WSMessage msg=JsonConvert.DeserializeObject<WSMessage>(message);
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(msg.Token);
            _webSockets[user.UserEmail] = socket;
            var buffer = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async Task BroadcastMessageAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            foreach (var socket in _webSockets.Values.Where(s => s.State == WebSocketState.Open))
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        public async Task WebSocketDisconnected(WebSocket ws)
        {
            var disconnectSocketId = "";
            foreach (var item in _webSockets.Keys)
            {
                if (_webSockets[item] == ws)
                {
                    disconnectSocketId = item;
                    break;
                }
            }

            _webSockets.Remove(disconnectSocketId);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            ws.Dispose();
        }

        public async Task RemoveWebSocketAsync(string id)
        {
            if (_webSockets.TryGetValue(id, out var socket))
            {
                _webSockets.Remove(id);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                socket.Dispose();
            }
        }
    }
    public class WSMessage
    {
        public string Token { get; set; }
        public string MsgType { get; set; }

    }
    

}
