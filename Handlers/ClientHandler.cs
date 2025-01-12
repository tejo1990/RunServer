using Microsoft.Extensions.Logging;
using RunServer.Constants;
using RunServer.Models;
using RunServer.Services.Database;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

public class ClientHandler : IDisposable
{
    private static readonly ConcurrentDictionary<string, string> LoggedInClients = new ConcurrentDictionary<string, string>();
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly IMySqlService _mySqlService;
    private readonly ILogger<ClientHandler> _logger;
    private const int BufferSize = 1024;
    private string LoggedInClientId { get; set; }

    public ClientHandler(TcpClient client, IMySqlService mySqlService, ILogger<ClientHandler> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = client.GetStream();
        _mySqlService = mySqlService ?? throw new ArgumentNullException(nameof(mySqlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        LoggedInClientId = "";
    }

    public async Task HandleClientAsync()
    {
        try
        {
            var buffer = new byte[BufferSize];
            var memoryStream = new MemoryStream();

            while (true)
            {

                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) // 클라이언트가 연결을 끊음
                {
                    _logger.LogInformation("클라이언트 연결이 종료되었습니다.");
                    break;
                }

                memoryStream.Write(buffer, 0, bytesRead);

                // 종료 문자 확인
                if (buffer[bytesRead - 1] == '\n')
                {
                    // 메시지 처리
                    var requestData = memoryStream.ToArray();
                    await ProcessRequestAsync(requestData);

                    // 메모리 스트림 초기화
                    memoryStream.SetLength(0);

                    // 수신 대기 중 로그
                    _logger.LogInformation("수신 대기 중...");
                }
            }
        }
        catch (Exception ex)
        {
            if (LoggedInClientId != null)
            {
                LoggedInClients.TryRemove(LoggedInClientId, out _);
                PrintLoggedInClients();
            }
            _logger.LogError(ex, "클라이언트 처리 중 오류 발생");
        }
        finally
        {

            Dispose();
        }
    }

    private async Task ProcessRequestAsync(byte[] data)
    {
        if (IsInvalidRequestData(data))
        {
            _logger.LogWarning("빈 요청 데이터 수신");
            return;
        }

        RequestModel? request = await DeserializeRequestAsync(data);
        
        if (request == null || string.IsNullOrWhiteSpace(request.Type))
        {
            _logger.LogWarning("잘못된 요청: 요청이 널이거나 타입이 없습니다.");
            await SendResponseAsync(new ResponseModel { Success = false, Message = "잘못된 요청" });
            return;
        }

        var response = await HandleRequestByTypeAsync(request);
        await SendResponseAsync(response);
    }

    private bool IsInvalidRequestData(byte[] data)
    {
        return data == null || data.Length == 0 || data.All(b => b == 0);
    }

    private async Task<RequestModel?> DeserializeRequestAsync(byte[] data)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<RequestModel>(new MemoryStream(data));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 역직렬화 중 오류 발생");
            await SendResponseAsync(new ResponseModel { Success = false, Message = "잘못된 요청 형식" });
            return null;
        }
    }

    private async Task<ResponseModel> HandleRequestByTypeAsync(RequestModel request)
    {
        return request.Type?.ToLower() switch
        {
            ServerConstants.RequestTypes.Login => await HandleLoginRequest(request),
            ServerConstants.RequestTypes.Status => await HandleStatusRequest(request),
            ServerConstants.RequestTypes.Echo => await HandleEchoRequest(request),
            ServerConstants.RequestTypes.Search => await HandleSearchRequest(request),
            ServerConstants.RequestTypes.Save => await HandleSaveRequest(request),
            ServerConstants.RequestTypes.List => await HandleCurrentLoggedInListRequest(),
            _ => new ResponseModel { Success = false, Message = "잘못된 요청 타입" }
        };
    }

    private async Task<ResponseModel> HandleLoginRequest(RequestModel request)
    {
        if (request.Data.TryGetValue("id", out var clientId))
        {
            _logger.LogInformation(clientId.ToString());
            // 데이터베이스에서 clientId 확인
            string? contentId = await _mySqlService.GetContentIdByClientIdAsync(clientId.ToString(), GlobalSettings.Table);
            if (contentId != null)
            {
                LoggedInClientId = clientId.ToString();
                LoggedInClients.TryAdd(clientId.ToString(), contentId);
                _logger.LogInformation("clientId : " + clientId + "추가 완료");

                PrintLoggedInClients();
            }
            return new ResponseModel { Success = true, Message = "로그인 성공" };
        }
        return new ResponseModel { Success = false, Message = "clientId가 없습니다." };
    }

    private async Task<ResponseModel> HandleStatusRequest(RequestModel request)
    {
        // 요청 데이터에서 "ping" 문자열 확인
        if (request.Data.TryGetValue("message", out var message) && message.ToString().ToLower() == "ping")
        {
            return new ResponseModel { Success = true, Message = "pong" };
        }
        return new ResponseModel { Success = false, Message = "잘못된 요청" };
    }

    private async Task<ResponseModel> HandleEchoRequest(RequestModel request)
    {
        // 에코 요청 처리
        if (request.Data.TryGetValue("message", out var message))
        {
            return new ResponseModel { Success = true, Message = $"echo: {message}" };
        }
        return new ResponseModel { Success = false, Message = "no message" };
    }

    private async Task<ResponseModel> HandleSearchRequest(RequestModel request)
    {
        // 검색 요청 처리
        if (request.Data.TryGetValue("id", out var info))
        {
            var results = await _mySqlService.GetItemByIdAsync(GlobalSettings.Table, info.ToString());
            return new ResponseModel { Success = true, Data = results };
        }
        return new ResponseModel { Success = false, Message = "There isn't requested id" };
    }

    private async Task<ResponseModel> HandleSaveRequest(RequestModel request)
    {
        // 저장 요청 처리
        if (request.Data.TryGetValue("id", out var userData))
        {
            var success = await _mySqlService.SaveUserDataAsync(GlobalSettings.Table, request.Data);
            return new ResponseModel { Success = success, Message = success ? "저장 성공" : "저장 실패" };
        }
        return new ResponseModel { Success = false, Message = "no data for save" };
    }

    private async Task<ResponseModel> HandleCurrentLoggedInListRequest()
    {
        try
        {
            // 현재 로그인된 클라이언트를 Dictionary<string, object> 형식으로 변환
            var loggedInClientsData = LoggedInClients.ToDictionary(
                entry => entry.Key,
                entry => (object)entry.Value
            );

            return new ResponseModel
            {
                Success = true,
                Data = loggedInClientsData // 변환된 데이터를 Data에 할당
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "현재 로그인된 클라이언트 목록을 가져오는 중 오류 발생");
            return new ResponseModel
            {
                Success = false,
                Message = "로그인된 클라이언트 목록을 가져오는 중 오류 발생"
            };
        }
    }

    private async Task SendResponseAsync(ResponseModel response)
    {
        // 응답을 클라이언트에 전송
        var responseData = JsonSerializer.Serialize(response);
        var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseData);
        await _stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        _logger.LogInformation("response : " + response.Message);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
    }

    internal void SendMessageAsync(string message)
    {
        throw new NotImplementedException();
    }

    private void PrintLoggedInClients()
    {
        Console.WriteLine("현재 로그인된 클라이언트 목록:");
        Console.WriteLine(new string('-', 40)); // 구분선
        Console.WriteLine($"{"User ID",-20}{"Content ID",-20}");
        Console.WriteLine(new string('-', 40)); // 구분선

        if (LoggedInClients.IsEmpty)
        {
            Console.WriteLine("현재 로그인된 클라이언트가 없습니다.");
        }
        else
        {
            foreach (var clientId in LoggedInClients.Keys)
            {
                // Content ID는 예시로 임의의 값을 사용합니다. 필요에 따라 실제 데이터를 사용하세요.
                _ = LoggedInClients.TryGetValue(clientId, out var contentIdValue) ? contentIdValue : "Unknown"; // 이 부분을 실제 데이터로 대체하세요.
                Console.WriteLine($"{clientId,-20}{contentIdValue,-20}");
            }
        }

        Console.WriteLine(new string('-', 40)); // 구분선
        Console.WriteLine($"총 로그인된 클라이언트 수: {LoggedInClients.Count}");
        Console.WriteLine();
    }
}