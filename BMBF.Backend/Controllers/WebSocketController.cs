using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BMBF.Backend.Models.Messages;
using BMBF.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BMBF.Backend.Controllers;

public class WebSocketController : ControllerBase
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
        
    private readonly ILogger _logger;
    private readonly IMessageService _messageService;
        
    public WebSocketController(IHostApplicationLifetime appLifetime, IMessageService messageService)
    {
        _appLifetime = appLifetime;
        _messageService = messageService;
        _logger = Log.Logger.ForContext<WebSocketController>();
    }

    [HttpGet("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.Information($"Accepting WebSocket connection from {HttpContext.Connection.RemoteIpAddress}");
            using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.Debug("Connection successful");
                
            var channel = Channel.CreateUnbounded<IMessage>();
            // TryWrite will always succeed, as the queue is unbounded
            void QueueMessageSend(IMessage message) => channel.Writer.TryWrite(message);

            _messageService.MessageSend += QueueMessageSend;
            try
            {
                await SendMessages(socket, channel, _appLifetime.ApplicationStopping);
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                _messageService.MessageSend -= QueueMessageSend;
            }

            _logger.Debug($"Disconnected from {HttpContext.Connection.RemoteIpAddress}");
        }
        else
        {
            HttpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        }
    }

    private async Task SendMessages(WebSocket webSocket, Channel<IMessage> messageChannel, CancellationToken ct)
    {
        // We do NOT simply pass the CancellationToken to ReceiveAsync, as this causes the WebSocket
        // to be immediately aborted when the token is canceled, which prevents us from properly
        // closing the connection.
        // ReSharper disable once AsyncVoidLambda (async void is safe as we catch the exception)
        await using var registration = ct.Register(async () =>
        {
            try
            {
                _logger.Debug("Sending close message");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "BMBFService shutting down", default);
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to properly close WebSocket: {ex.Message}");
            }
        });
            
        var readBuffer = WebSocket.CreateServerBuffer(1024);
        using var msgStream = new MemoryStream();
        Task<WebSocketReceiveResult>? receiveTask = null;
        Task<IMessage>? messageTask = null;
        while(webSocket.State == WebSocketState.Open)
        {
            // Wait until either we receive another message to send to the frontend, or we receive a message from the WebSocket
            receiveTask ??= webSocket.ReceiveAsync(readBuffer, default);
            messageTask ??= messageChannel.Reader.ReadAsync(ct).AsTask();
            await Task.WhenAny(receiveTask, messageTask);
                
            if (receiveTask.IsCompleted)
            {
                var message = await receiveTask;
                receiveTask = null;
                // We do not support receiving messages at this time
                if (message.CloseStatus == null)
                {
                    _logger.Warning($"Received non-closing message from {HttpContext.Connection.RemoteIpAddress}");
                }
                else
                {
                    // A close message has been received, so the loop will now exit, since the state will no longer be Open
                    continue;
                }
            }

            if (messageTask.IsCompleted)
            {
                var message = await messageTask;
                messageTask = null;

                // Make sure to overwrite the (potentially) existing message in the buffer
                msgStream.Position = 0;
                    
                // No need for SerializeAsync - we are writing to a MemoryStream
                JsonSerializer.Serialize(msgStream, message, _serializerOptions);

                int length = (int) msgStream.Position;

                await webSocket.SendAsync(
                    // Select only the portion of the buffer for this message
                    msgStream.GetBuffer()[..length],
                    WebSocketMessageType.Text,
                    true,
                    ct
                );
            }
        }
    }
}