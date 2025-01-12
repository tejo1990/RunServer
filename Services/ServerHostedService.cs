using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using RunServer.Services.Socket;
using RunServer.Services.Database;

namespace RunServer.Services
{
    public class ServerHostedService : IHostedService
    {
        private readonly ILogger<ServerHostedService> _logger;
        private readonly ISocketService _socketService;
        private readonly IMySqlService _mySqlService;
        private readonly IConfiguration _configuration;

        public ServerHostedService(
            ILogger<ServerHostedService> logger,
            ISocketService socketService,
            IMySqlService mySqlService,
            IConfiguration configuration)
        {
            _logger = logger;
            _socketService = socketService;
            _mySqlService = mySqlService;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("서버 시작중...");

            string? ip = _configuration["ServerSettings:IP"];
            int? port = int.Parse(_configuration["ServerSettings:Port"]);

            // 데이터베이스 연결 시도
            if (!await _mySqlService.ConnectAsync())
            {
                _logger.LogError("데이터베이스 연결에 실패했습니다. 서버를 시작할 수 없습니다.");
                return;
            }

            // 데이터베이스에서 정보 가져오기
            var tableName = _configuration["ServerSettings:Table"];
            var items = await _mySqlService.GetAllItemsAsync(tableName);

            foreach (var item in items)
            {
                Console.WriteLine("데이터베이스 항목:");
                foreach (var kvp in item)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("----------------------------");
            }

            await _socketService.StartAsync(ip ?? "1.1.1.1", port ?? 0000);

            _logger.LogInformation($"서버가 {ip}:{port}에서 시작되었습니다.");



            // 데이터 출력

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("서버 종료중...");
            return _socketService.StopAsync();
        }
    }
}