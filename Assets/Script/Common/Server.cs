using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public InputField PortInput;
    private List<ServerClient> clients;

    private TcpListener server;
    private bool serverStarted;

    private Quiz currentQuiz; // 현재 퀴즈
    private int quizCount = 0; // 퀴즈 출제 횟수 변수

    List<Lobby> lobbies = new List<Lobby>();
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // 한 번에 하나만 실행

    public static Server Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 다른 씬 로드 시에도 유지
        }
        else
        {
            Destroy(gameObject); // 중복된 QuizManager 삭제
        }
    }

    private void Start()
    {
        quizCount = 0;
    }

    public void ServerCreate()
    {
        clients = new List<ServerClient>();

        try
        {
            int port = string.IsNullOrEmpty(PortInput.text) ? 7777 : int.Parse(PortInput.text);
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;

            //Chat.instance.ShowMessage($"서버가 {port}에서 시작되었습니다.");
            Debug.Log($"서버가 {port}에서 시작되었습니다.");
        }
        catch (Exception e)
        {
            //Chat.instance.ShowMessage($"소켓 에러: {e.Message}");
            Debug.LogError("소켓 에러");
        }
    }

    private void Update()
    {
        if (!serverStarted) return;


        foreach (Lobby lob in lobbies)
        {
            for (int i = lob.clients.Count - 1; i >= 0; i--)
            {
                ServerClient c = lob.clients[i];

                //플레이어 연결 종료
                if (!IsConnected(c.tcp))
                {
                    Debug.Log($"{c.clientName}이 연결되지 않습니다.");
                    lob.RemovePlayer(c);

                    UpdateList(lob, c);         //UI 업데이트
                    CheckReadyBtn(lob, false);  //준비버튼 활성화 비활성화 체크
                    CheckStart(lob);            //그 뒤 게임 시작 검사

                    c.tcp.Close();
                    continue;
                }

                NetworkStream s = c.tcp.GetStream();
                if (s.DataAvailable)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = s.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        OnIncomingData(c, data, lob);
                    }
                }
            }
        }
    }

    private bool IsConnected(TcpClient c)
    {
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    private async void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        ServerClient newClient = new ServerClient(listener.EndAcceptTcpClient(ar));

        Debug.Log($"새 클라이언트 접속");
        await ClientJoinAsync(newClient);

        //clients.Add(newClient);
        StartListening();
    }

    private void OnIncomingData(ServerClient c, string data, Lobby lob)
    {
        DataPacket packet = DataPacket.Deserialize(Encoding.UTF8.GetBytes(data));

        if (packet.Type == "message")
        {
            string clientMessage = packet.Value;
            if (currentQuiz != null)
            {
                bool isCorrect = QuizManager.Instance.CheckAnswer(clientMessage);
                if (isCorrect)
                {
                    OnQuizAnsweredCorrectly(c.clientName, currentQuiz.answer);
                }
                else
                {
                    BroadcastMessage($"{c.clientName}: {clientMessage}");
                }
            }
            else
            {
                BroadcastMessage($"{c.clientName}: {clientMessage}");
            }
        }
        else if (packet.Type == "nickname")
        {
            c.clientName = packet.Value;
            BroadcastMessage($"{c.clientName}님이 입장했습니다.");
        }
        else if (packet.Type == "ready")
        {
            if (bool.TryParse(packet.Value, out bool ready))
            {
                c.isReady = ready;
                Debug.Log($"{c.clientName}의 준비 상태 변경 : {ready}");
            }
            else
            {
                Debug.LogError($"잘못된 데이터 형식: {packet.Value}");
                return;
            }

            CheckStart(lob);
        }
    }

    private void Broadcast(byte[] data)
    {
        foreach (Lobby lob in lobbies)
        {
            for (int i = lob.clients.Count - 1; i >= 0; i--)
            {
                ServerClient c = lob.clients[i];

                SendMessage(c, data);
            }
        }
    }

    private void SendMessage(ServerClient c, byte[] data)
    {
        try
        {
            NetworkStream stream = c.tcp.GetStream();
            byte[] delimiter = Encoding.UTF8.GetBytes("\n"); // 패킷 구분자
            byte[] dataWithDelimiter = new byte[data.Length + delimiter.Length];

            Buffer.BlockCopy(data, 0, dataWithDelimiter, 0, data.Length);
            Buffer.BlockCopy(delimiter, 0, dataWithDelimiter, data.Length, delimiter.Length);

            stream.Write(dataWithDelimiter, 0, dataWithDelimiter.Length);
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"쓰기 에러: {e.Message} - 클라이언트 {c.clientName}");
        }
    }

    // 서버에서 메시지 전송
    public new void BroadcastMessage(string message)
    {
        DataPacket messagePacket = new DataPacket("message", message);
        byte[] serializedMessage = messagePacket.Serialize();
        Broadcast(serializedMessage);
    }








    #region 로비 시스템
    // 최초 클라이언트 접속 처리
    public async Task ClientJoinAsync(ServerClient client)
    {
        foreach (var lob in lobbies)
        {
            if (!lob.isFull)
            {
                ConnectLobby(lob, client);
                return;
            }
        }

        ConnectLobby(await CreateLobbyAsync(), client);
    }

    // 비동기 로비 생성
    public async Task<Lobby> CreateLobbyAsync()
    {
        await semaphore.WaitAsync(); // 세마포로 락 획득

        try
        {
            Lobby newLob = new Lobby(Guid.NewGuid().ToString());  // 고유 아이디 부여
            lobbies.Add(newLob);

            Debug.Log($"로비 {newLob.lobbyId} 생성 완료.");
            return newLob;
        }
        finally
        {
            semaphore.Release(); // 락 해제
        }
    }

    // 비동기 로비 삭제
    public async Task RemoveLobbyAsync()
    {
        await semaphore.WaitAsync(); // 세마포로 락 획득

        try
        {
            for (int i = lobbies.Count - 1; i >= 0; i--)
            {
                if (lobbies[i].clients.Count == 0)
                {
                    Debug.Log($"{lobbies[i].lobbyId} 로비가 삭제되었습니다.");
                    lobbies.RemoveAt(i);
                }
            }
        }
        finally
        {
            semaphore.Release(); // 락 해제
        }
    }

    // 비동기 로비와 클라이언트 연결
    public void ConnectLobby(Lobby lob, ServerClient client)
    {
        if (lob.isFull)
        {
            Debug.LogError("로비 에러 : 이미 차있는 로비에 접속을 시도했습니다.");
            return;
        }

        Debug.Log($"{lob.lobbyId} 로비에 {client.clientName} 클라이언트가 연결되었습니다.");
        lob.AddPlayer(client);

        //준비버튼 활성화 비활성화 체크
        CheckReadyBtn(lob, true);

        //todo - 클라에 신호 보내서 씬이동 시켜야됨
        //로비로 이동한 후에 로비업데이트가 이루어져야 함...
        //Type을 movetolobby로 하고 data를 플레이어 이름 목록으로 해놓고 전송
        //그럼 클라에서 씬이동 하고 씬이동 끝나야 data 쓰도록 작성

        UpdateList(lob, client);
    }

    private void CheckStart(Lobby lob)
    {
        if (lob.clients.All(client => client.isReady))
        {
            Debug.Log("모든 클라이언트가 준비되었습니다. 게임을 시작합니다.");
            // TODO: 게임 시작 로직
        }
    }

    private void UpdateList(Lobby lob, ServerClient client)
    {
        DataPacket messagePacket = new DataPacket("listUpdate", ExportNames(lob));
        byte[] serializedMessage = messagePacket.Serialize();

        foreach (ServerClient c in lob.clients)
        {
            //당사자는 따로 처리됨
            if (c == client)
                continue;

            Debug.Log($"{lob.lobbyId} 로비의 {c.clientName} 클라이언트에게 데이터를 전송합니다.");
            SendMessage(c, serializedMessage);
        }
    }

    private void CheckReadyBtn(Lobby lob, bool isPlus)
    {
        int count = lob.clients.Count;

        //사람이 늘었다면
        if (isPlus && count == 2)
        {
            UpdateReadyBtn(lob, true);
        }
        //사람이 줄었다면
        else if (!isPlus && count == 1)
        {
            lob.clients[0].isReady = false;
            UpdateReadyBtn(lob, false);
        }
    }

    private void UpdateReadyBtn(Lobby lob, bool active)
    {
        DataPacket messagePacket = new("readyBtn", active.ToString());
        byte[] serializedMessage = messagePacket.Serialize();

        foreach (ServerClient c in lob.clients)
        {
            Debug.Log($"{lob.lobbyId} 로비의 {c.clientName} 클라이언트의 준비버튼 상태를 변경합니다.");
            SendMessage(c, serializedMessage);
        }
    }

    private string ExportNames(Lobby lob)
    {
        // 로비에 있는 클라이언트의 이름을 추출
        return string.Join(", ", lob.clients.Select(c => c.clientName.ToString()));
    }

    #endregion









    #region 퀴즈 시스템
    // 서버에서 퀴즈 전송
    public void BroadcastQuiz(int quizId, string question)
    {
        string quizData = $"{quizId}|{question}";
        DataPacket quizPacket = new DataPacket("quiz", quizData);
        byte[] serializedQuiz = quizPacket.Serialize();
        Broadcast(serializedQuiz);
    }

    // 특정 클라이언트에게 점수 전송
    private void BroadcastScore(ServerClient client)
    {
        string scoreData = $"{client.clientName}|{client.score}";
        DataPacket scorePacket = new DataPacket("score", scoreData);
        byte[] serializedScore = scorePacket.Serialize();
        Broadcast(serializedScore);
    }

    // 모든 클라이언트에게 점수 브로드캐스트
    public void BroadcastAllScores()
    {
        foreach (var client in clients)
        {
            BroadcastScore(client);
        }
    }
    private void BroadcastCountdown(int countdownTime)
    {
        // 카운트다운 데이터를 클라이언트에게 전송
        DataPacket countdownPacket = new DataPacket("countdown", countdownTime.ToString());
        byte[] serializedCountdown = countdownPacket.Serialize();
        Broadcast(serializedCountdown); // 모든 클라이언트에게 카운트다운 전송
    }

    // 정답 맞춘 경우 처리 및 새 퀴즈 전송
    public void OnQuizAnsweredCorrectly(string nickname, string answer)
    {
        // 정답 메시지 전송
        string message = $"{nickname}님이 정답을 맞췄습니다! 정답: {answer}";
        Debug.Log(message);
        BroadcastMessage(message);

         // 정답자를 찾아 점수 증가 
        foreach (var client in clients)
        {
            if (client.clientName == nickname)
            {
                client.score += 10; // 점수 10점 추가
                Debug.Log($"{nickname}의 점수: {client.score}");
                //BroadcastAllScores();
                //BroadcastScore(client); // 점수 업데이트 전송
                break;
            }
        }

        BroadcastAllScores(); // 전체 클라이언트에게 점수 전송

        //BroadcastNextQuiz();
        StartCoroutine(CountdownAndBroadcastQuiz());
    }


    
    // 시작버튼 누르면 문제 출력되는 함수
    public void OnBroadcastQuizButton()
    {
        SceneController.Instance.LoadScene("QuizScene");
        if (!serverStarted)
        {
            Debug.Log("서버가 시작되지 않았습니다!");
            return;
        }

        //BroadcastNextQuiz();
        StartCoroutine(CountdownAndBroadcastQuiz());
    }

    private void BroadcastNextQuiz() 
    {
        Quiz nextQuiz = QuizManager.Instance.GetRandomQuiz();
        if (nextQuiz == null)
        {
            BroadcastMessage("퀴즈가 모두 종료되었습니다!");
            return;
        }

        currentQuiz = nextQuiz;
        quizCount++;
        BroadcastQuiz(nextQuiz.id, nextQuiz.question.Replace(";", ","));
        Debug.Log($"출제된 퀴즈: {nextQuiz.question}");
        QuizManager.Instance.isStartQuiz = true;
    }

    // 카운트다운 후 퀴즈를 브로드캐스트하는 코루틴
    private IEnumerator CountdownAndBroadcastQuiz()
    {
        yield return new WaitForSeconds(0.5f); // 로딩시간
        QuizManager.Instance.isStartQuiz = false;

        if(quizCount>=10)
        {
            QuizManager.Instance.QuizText.text = "퀴즈가 종료되었습니다!";

            yield return new WaitForSeconds(1.0f);
            quizCount = 0;
            SceneController.Instance.LoadScene("ResultScene");
        }
        else
        {
            int countdownTime = 3; // 3초 카운트다운
            while (countdownTime > 0)
            {
                QuizManager.Instance.CountdownText.text = countdownTime.ToString(); // UI 업데이트
                BroadcastCountdown(countdownTime);
                Debug.Log($"카운트다운: {countdownTime}");
                yield return new WaitForSeconds(1); // 1초 대기
                countdownTime--;
            }

            QuizManager.Instance.CountdownText.text = ""; // 카운트다운 종료 후 텍스트 제거
            BroadcastNextQuiz(); // 퀴즈 브로드캐스트

        }
    }
    #endregion






    #region 테스트용 함수
    public void CreateEmptyLobby()
    {
        Lobby newLob = new Lobby(Guid.NewGuid().ToString());  // 고유 아이디 부여
        lobbies.Add(newLob);

        Debug.Log($"빈 로비 {newLob.lobbyId} 생성 완료.");
    }

    public void RemoveEmptyLobby()
    {
        for (int i = lobbies.Count - 1; i >= 0; i--)
        {
            if (lobbies[i].clients.Count == 0)
            {
                Debug.Log($"{lobbies[i].lobbyId} 로비가 삭제되었습니다.");
                lobbies.RemoveAt(i);
            }
        }
    }

    // 로비 목록 호출
    public void GetLobbyList()
    {
        foreach (var lobby in lobbies)
        {
            Debug.Log($"로비 ID: {lobby.lobbyId}, 인원 수: {lobby.clients.Count}");
        }
    }
    #endregion


}

public class ServerClient
{
    public TcpClient tcp;
    public string clientName;
    public int score; // 점수
    public bool isReady = false;

    public ServerClient(TcpClient clientSocket)
    {
        clientName = "Guest";
        tcp = clientSocket;
        score = 0;

    }
}
