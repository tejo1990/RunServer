using Microsoft.Extensions.Logging;
using RunServer.Services.Database;
using System.Collections.Concurrent;
using System.Net.Sockets;

public class ClientService : IClientService
{
    private readonly ILogger<ClientService> _logger;
    private readonly IMySqlService _mySqlService;
    private readonly ConcurrentDictionary<string, ClientHandler> _clients;
    private readonly ILogger<ClientHandler> _clientHandlerLogger;

    public ClientService(ILogger<ClientService> logger, IMySqlService mySqlService, ILogger<ClientHandler> clientHandlerLogger)
    {
        _logger = logger;
        _mySqlService = mySqlService;
        _clients = new ConcurrentDictionary<string, ClientHandler>();
        _clientHandlerLogger = clientHandlerLogger;
    }

    public async Task HandleNewClientAsync(TcpClient tcpClient)
    {
        var handler = new ClientHandler(tcpClient, _mySqlService, _clientHandlerLogger);
        await handler.HandleClientAsync();
    }

    public void PrintConnectedClientIds()
    {
        foreach (var clientId in _clients.Keys)
        {
            Console.WriteLine($"Connected Client ID: {clientId}");
        }
    }

    public int GetConnectedClientsCount() => _clients.Count;

    public bool IsClientConnected(string clientId) => _clients.ContainsKey(clientId);

    public Task BroadcastMessageAsync(string message)
    {
        foreach (var client in _clients.Values)
        {
            client.SendMessageAsync(message);
        }
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(string message)
    {
        foreach (var client in _clients.Values)
        {
            client.SendMessageAsync(message);
        }
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(string message, string clientId)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            client.SendMessageAsync(message);
        }
        return Task.CompletedTask;
    }

    // public Task SendMessageAsync(string message, List<string> clientIds)
    // {
    //     foreach (var clientId in clientIds)
    //     {
    //         SendMessageAsync(message, clientId);
    //     }
    //     return Task.CompletedTask;
    // }

    // public Task SendMessageAsync(string message, string clientId)
    // {
    //     if (_clients.TryGetValue(clientId, out var client))
    //     {
    //         client.SendMessageAsync(message);
    //     }
    //     return Task.CompletedTask;
    // }

    // public Task SendMessageAsync(string message, List<string> clientIds)
    // {
    //     foreach (var clientId in clientIds)
    //     {
    //         SendMessageAsync(message, clientId);
    //     }
    //     return Task.CompletedTask;
    // }


}