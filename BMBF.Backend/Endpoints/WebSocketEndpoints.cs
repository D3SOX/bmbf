using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BMBF.Backend.Models.Messages;
using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BMBF.Backend.Endpoints;

public class WebSocketEndpoints : IEndpoints
{
    private readonly IMessageService _messageService;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly JsonSerializerOptions _serializerOptions;

    public WebSocketEndpoints(IMessageService messageService, IHostApplicationLifetime appLifetime, JsonSerializerOptions serializerOptions)
    {
        _messageService = messageService;
        _appLifetime = appLifetime;
        _serializerOptions = serializerOptions;
    }

    [HttpGet("/ws")]
    public HttpResponse ConnectWebSocket(Request request)
    {
        return WebSocket.Response(request.Inner, Handler);
    }

    private async Task Handler(WebSocket socket)
    {
        var channel = Channel.CreateUnbounded<IMessage>();
        void QueueMessageSend(IMessage message) => channel.Writer.TryWrite(message);

        _messageService.MessageSend += QueueMessageSend;
        try
        {
            await SendMessages(socket, channel, _appLifetime.ApplicationStopping);
        }
        catch (OperationCanceledException)
        {

        }
        catch (InvalidOperationException)
        {
            Log.Verbose("WebSocket disconnected");
        }
        finally
        {
            _messageService.MessageSend -= QueueMessageSend;
            await socket.Close(new WebSocketCloseMessage(Reason: "BMBFService shutting down"));
        }
    }

    private async Task SendMessages(WebSocket webSocket, Channel<IMessage> messageChannel, CancellationToken ct)
    {
        Task<WebSocketMessage?>? receiveTask = null;
        Task<IMessage>? messageTask = null;
        while (true)
        {
            receiveTask ??= webSocket.Receive(ct);
            messageTask ??= messageChannel.Reader.ReadAsync(ct).AsTask();
            await Task.WhenAny(receiveTask, messageTask);

            if (receiveTask.IsCompleted)
            {
                var message = await receiveTask;
                receiveTask = null;
                if (message == null)
                {
                    // A close message has been received!
                    break;
                }

                // We do not support receiving messages at this time
                Log.Warning($"Received non-closing message from {webSocket.Remote}");
            }
            if (messageTask.IsCompleted)
            {
                var message = await messageTask;
                messageTask = null;

                string msg = JsonSerializer.Serialize(message, message.GetType(), _serializerOptions);
                await webSocket.Send(new WebSocketTextMessage(msg), ct);
            }
        }
    }
}
