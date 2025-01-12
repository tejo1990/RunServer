using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace RunServer.Services.Socket
{
    public class SocketService : ISocketService
    {
        private readonly IClientService _clientService;
        private readonly ILogger<SocketService> _logger;
        private TcpListener _server;
        private bool _isRunning;

        public SocketService(IClientService clientService, ILogger<SocketService> logger = null)
        {
            _clientService = clientService;
            _logger = logger;
        }

        public async Task StartAsync(string ip, int port)
        {
            _server = new TcpListener(IPAddress.Parse(ip), port);
            _server.Start();
            _isRunning = true;

            _logger?.LogInformation($"서버가 {ip}:{port}에서 시작되었습니다.");

            while (_isRunning)
            {
                var client = await _server.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        // ... HandleClientAsync 및 기타 메서드 구현
        
        public void Dispose()
        {
            _server?.Stop();
        }

        public Task HandleClientAsync(TcpClient client)
        {
            _clientService.HandleNewClientAsync(client);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }
    }
}