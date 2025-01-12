using System.Net.Sockets;
using System.Threading.Tasks;

public interface IClientService
{
    /// <summary>
    /// 새로운 TCP 클라이언트 연결을 처리합니다.
    /// </summary>
    /// <param name="tcpClient">처리할 TCP 클라이언트 인스턴스</param>
    Task HandleNewClientAsync(TcpClient tcpClient);

    /// <summary>
    /// 현재 연결된 클라이언트의 수를 반환합니다.
    /// </summary>
    /// <returns>연결된 클라이언트 수</returns>
    int GetConnectedClientsCount();

    /// <summary>
    /// 특정 클라이언트 ID에 해당하는 클라이언트가 연결되어 있는지 확인합니다.
    /// </summary>
    /// <param name="clientId">확인할 클라이언트 ID</param>
    /// <returns>클라이언트 연결 여부</returns>
    bool IsClientConnected(string clientId);

    /// <summary>
    /// 모든 연결된 클라이언트에게 메시지를 브로드캐스트합니다.
    /// </summary>
    /// <param name="message">브로드캐스트할 메시지</param>
    Task BroadcastMessageAsync(string message);
} 