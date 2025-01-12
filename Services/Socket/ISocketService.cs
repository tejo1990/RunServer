using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RunServer.Services.Socket
{
    public interface ISocketService : IDisposable
    {
        Task StartAsync(string ip, int port);
        Task HandleClientAsync(TcpClient client);
        Task StopAsync();
    }
}