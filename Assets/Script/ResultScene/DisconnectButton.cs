using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectButton : MonoBehaviour
{
    private TcpClient client; // 서버 연결 객체
    private NetworkStream stream; // 서버와의 통신 스트림
    private bool lastConnectionStatus = false; // 이전 연결 상태를 저장
    private bool initialized = false; // 초기화 상태

    void Update()
    {
        if (!initialized)
        {
            TryInitializeClient();
        }

        if (client == null) return;

        bool isCurrentlyConnected = IsConnected();

        // 연결 상태가 변경되었을 때만 로그 출력
        if (isCurrentlyConnected != lastConnectionStatus)
        {
            lastConnectionStatus = isCurrentlyConnected;

            if (isCurrentlyConnected)
            {
                Debug.Log("[DISCONNECT BUTTON] 서버와의 연결이 활성화되었습니다.");
            }
            else
            {
                Debug.Log("[DISCONNECT BUTTON] 서버와의 연결이 끊어졌습니다.");
            }
        }
    }

    private void TryInitializeClient()
    {
        // Client 인스턴스 찾기
        Client clientInstance = FindObjectOfType<Client>();
        if (clientInstance == null)
        {
            Debug.LogWarning("[DISCONNECT BUTTON] Client 인스턴스를 찾을 수 없습니다. 계속 대기 중...");
            return;
        }

        // Client 인스턴스의 TcpClient를 가져오기 위한 간접 접근
        var clientField = clientInstance.GetType().GetField("socket",
                          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (clientField != null)
        {
            client = clientField.GetValue(clientInstance) as TcpClient;

            if (client != null)
            {
                stream = client.GetStream();
                initialized = true;
                Debug.Log("[DISCONNECT BUTTON] TcpClient와 NetworkStream 초기화 성공.");
            }
            else
            {
                Debug.Log("[DISCONNECT BUTTON] TcpClient를 가져오지 못했습니다.");
            }
        }
        else
        {
            Debug.Log("[DISCONNECT BUTTON] Client 클래스에서 TcpClient를 찾을 수 없습니다.");
        }
    }

    public void OnDisconnectButtonClicked()
    {
        if (IsConnected())
        {
            Debug.Log("[DISCONNECT BUTTON] 서버와 연결되어 있습니다. 연결을 종료합니다.");
            DisconnectFromServer();
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.LogWarning("[DISCONNECT BUTTON] 이미 연결이 끊어진 상태입니다.");
        }
    }

    private void DisconnectFromServer()
    {
        Debug.Log("[DISCONNECT BUTTON] DisconnectFromServer 실행 중...");

        if (stream != null)
        {
            try
            {
                stream.Close();
                Debug.Log("[DISCONNECT BUTTON] 스트림 종료 성공");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DISCONNECT BUTTON] 스트림 종료 오류: {e.Message}");
            }
        }

        if (client != null)
        {
            try
            {
                client.Close();
                Debug.Log("[DISCONNECT BUTTON] 서버 연결이 정상적으로 종료되었습니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DISCONNECT BUTTON] 클라이언트 종료 오류: {e.Message}");
            }
        }
    }

    private bool IsConnected()
    {
        try
        {
            if (client != null && client.Connected)
            {
                return stream != null && stream.CanRead;
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"[DISCONNECT BUTTON] 연결 상태 확인 중 오류 발생: {e.Message}");
        }

        return false;
    }
}
