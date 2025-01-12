using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RunServer.Services.Database;
using RunServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RunServer.Services.Socket;
using Microsoft.Extensions.Configuration;

namespace RunServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

            var configuration = new ConfigurationBuilder()
                .SetBasePath(projectDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            GlobalSettings.Initialize(configuration);

            Console.WriteLine($"IP: {GlobalSettings.IP}");
            Console.WriteLine($"Port: {GlobalSettings.Port}");
            Console.WriteLine($"DB Server: {GlobalSettings.Server}");
            Console.WriteLine($"DB Database: {GlobalSettings.Database}");
            Console.WriteLine($"DB UID: {GlobalSettings.Uid}");
            Console.WriteLine($"DB PWD: {GlobalSettings.Password}");
            Console.WriteLine($"DB Table: {GlobalSettings.Table}");
            Console.WriteLine($"Connection String: {GlobalSettings.ConnectionString}");

            var services = new ServiceCollection();

            // 핵심 서비스 등록
            services.AddSingleton<IClientService, ClientService>();
            services.AddSingleton<IMySqlService>(provider => 
                new MySqlService(GlobalSettings.ConnectionString, provider.GetRequiredService<ILogger<MySqlService>>()));
            services.AddSingleton<IHostedService, ServerHostedService>();
            services.AddSingleton<ISocketService, SocketService>();

            // 로깅 설정
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var serviceProvider = services.BuildServiceProvider();

            // 서버 시작
            var logger = serviceProvider.GetRequiredService<ILogger<ServerHostedService>>();
            var socketService = serviceProvider.GetRequiredService<ISocketService>();
            var mySqlService = serviceProvider.GetRequiredService<IMySqlService>();
            var server = new ServerHostedService(logger, socketService, mySqlService, configuration);
            await server.StartAsync(CancellationToken.None);

            // 프로그램 실행 유지
            await Task.Delay(-1);
        }
    }

}
