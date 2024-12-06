using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;

public class Rank : MonoBehaviour
{
    private TcpClient client; // 클라이언트 객체
    private NetworkStream stream; // 네트워크 스트림
    private bool isConnected = false; // 서버 연결 상태 플래그
    private bool initialized = false; // 초기화 상태 플래그

    public Text rankDisplay; // UI 텍스트 컴포넌트

    void Start()
    {
        TryInitializeClient(); // Client 초기화 시도
    }

    void Update()
    {
        if (!initialized)
        {
            TryInitializeClient(); // Client 초기화 재시도
        }

        if (isConnected && stream != null && stream.DataAvailable)
        {
            try
            {
                // 서버로부터 데이터 수신
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    List<ServerClient> players = JsonConvert.DeserializeObject<List<ServerClient>>(data);

                    // 수신한 데이터를 UI에 출력
                    DisplayRankings(players);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Rank] 데이터 수신 실패: {e.Message}");
            }
        }
    }

    private void TryInitializeClient()
    {
        // Client 인스턴스 찾기
        Client clientInstance = FindObjectOfType<Client>();
        if (clientInstance == null)
        {
            Debug.LogWarning("[Rank] Client 인스턴스를 찾을 수 없습니다. 계속 대기 중...");
            return;
        }

        // Reflection을 통해 비공개 필드 접근
        var clientField = clientInstance.GetType().GetField("socket",
                          BindingFlags.NonPublic | BindingFlags.Instance);

        if (clientField != null)
        {
            client = clientField.GetValue(clientInstance) as TcpClient;

            if (client != null)
            {
                stream = client.GetStream();
                isConnected = true;
                initialized = true;
                Debug.Log("[Rank] TcpClient와 NetworkStream 초기화 성공.");
            }
            else
            {
                Debug.Log("[Rank] TcpClient를 가져오지 못했습니다.");
            }
        }
        else
        {
            Debug.Log("[Rank] Client 클래스에서 TcpClient를 찾을 수 없습니다.");
        }
    }

    private void DisplayRankings(List<ServerClient> players)
    {
        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("[Rank] 플레이어 데이터가 없습니다.");
            rankDisplay.text = "플레이어 데이터가 없습니다.";
            return;
        }

        // 플레이어 점수 내림차순 정렬
        var sortedPlayers = players.OrderByDescending(player => player.score).ToList();
        StringBuilder rankings = new StringBuilder();

        rankings.AppendLine("현재 랭킹:");
        foreach (var player in sortedPlayers)
        {
            rankings.AppendLine($"{player.clientName}: {player.score}점");
        }

        // UI 텍스트에 랭킹 표시
        rankDisplay.text = rankings.ToString();
    }

    void OnApplicationQuit()
    {
        if (stream != null)
        {
            stream.Close();
        }

        if (client != null)
        {
            client.Close();
        }
    }
}
