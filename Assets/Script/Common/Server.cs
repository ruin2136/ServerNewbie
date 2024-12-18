﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public InputField PortInput;

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
        try
        {
            int port = string.IsNullOrEmpty(PortInput.text) ? 7777 : int.Parse(PortInput.text);
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;

            Debug.Log($"서버가 {port}에서 시작되었습니다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"소켓 에러: {e.Message}");
        }
    }

    private void Update()
    {
        if (!serverStarted) return;

        foreach (Lobby lob in lobbies)
        {
            if (lob == null || lob.clients == null)
                return;

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
                    OnQuizAnsweredCorrectly(c, c.clientName, currentQuiz.answer);
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
            Debug.Log($"{lob.lobbyId} 로비의 신규 클라 이름 설정 : {c.clientName}");

            // 로비 씬으로 이동 신호 전송
            DataPacket movetoLobbyPacket = new("moveToLobby", ExportNames(lob));
            byte[] serializedData = movetoLobbyPacket.Serialize();
            SendMessageToClient(c, serializedData);

            //해당 로비의 다른 플레이어들 UI 갱신 신호 전송
            UpdateList(lob, c);
            //준비버튼 활성화 비활성화 체크
            CheckReadyBtn(lob, true);

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

                SendMessageToClient(c, data);
            }
        }
    }

    private void SendMessageToClient(ServerClient c, byte[] data)
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
            if (!lob.isFull && !lob.isStart)
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

        Debug.Log($"{lob.lobbyId} 로비에 클라이언트가 연결되었습니다.");
        lob.AddPlayer(client);
    }

    private void CheckStart(Lobby lob)
    {
        if (lob.clients.All(client => client.isReady) && lob.clients.Count > 1)
        {
            Debug.Log("모든 클라이언트가 준비되었습니다. 게임을 시작합니다.");
            // TODO: 게임 시작 로직
            // 이름과 점수를 직렬화 (예: "Alice,10;Bob,20;Charlie,15")
            lob.isStart = true;

            string playerData = string.Join(";", lob.clients.Select(client => $"{client.clientName},{client.score}"));

            DataPacket moveToQuizPacket = new("moveToQuiz", playerData);
            byte[] serializedData = moveToQuizPacket.Serialize();

            // 모든 클라이언트에게 패킷 전송
            foreach (var client in lob.clients)
            {
                SendMessageToClient(client, serializedData);
            }
            OnBroadcastQuizButton();

        }
    }

    //해당 로비의 정보 갱신
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
            SendMessageToClient(c, serializedMessage);
        }
    }

    private void CheckReadyBtn(Lobby lob, bool isPlus)
    {
        int count = lob.clients.Count;

        //사람이 늘었다면
        if (isPlus && count >= 2)
        {
            Debug.Log("사람이 늘었음");
            UpdateReadyBtn(lob, true);
        }
        //사람이 줄었다면
        else if (!isPlus && count == 1)
        {
            Debug.Log("사람이 줄었음");
            lob.clients[0].isReady = false;
            UpdateReadyBtn(lob, false);
        }
    }

    private void UpdateReadyBtn(Lobby lob, bool active)
    {
        Debug.Log($"서버 : 버튼 상태 업데이트 to {active}");
        DataPacket messagePacket = new("readyBtn", active.ToString());
        byte[] serializedMessage = messagePacket.Serialize();

        foreach (ServerClient c in lob.clients)
        {
            Debug.Log($"{lob.lobbyId} 로비의 {c.clientName} 클라이언트의 준비버튼 상태를 변경합니다.");
            SendMessageToClient(c, serializedMessage);
        }
    }

    // 해당 로비의 이름 추출
    private string ExportNames(Lobby lob)
    {
        return string.Join(", ", lob.clients.Select(c => c.clientName.ToString()));
    }


    #endregion








    #region 퀴즈 시스템
    // 서버에서 퀴즈 전송
    public void BroadcastQuiz(int quizId, string question, string answer)
    {
        string quizData = $"{quizId}|{question}|{answer}";
        DataPacket quizPacket = new DataPacket("quiz", quizData);
        byte[] serializedQuiz = quizPacket.Serialize();
        Broadcast(serializedQuiz);

        Debug.Log($"퀴즈 전송: ID={quizId}, Question={question}, 답 = {answer}");
    }

    // 모든 클라이언트에게 점수 브로드캐스트
    public void BroadcastAllScores(Lobby lob)
    {
        // 클라이언트의 이름과 점수를 포함하는 데이터 생성
        string scoreData = string.Join(";",
            lob.clients.Select(client => $"{client.clientName},{client.score}"));

        // 점수 데이터를 포함한 패킷 생성
        DataPacket scoreUpdatePacket = new DataPacket("score", scoreData);
        byte[] serializedData = scoreUpdatePacket.Serialize();

        // 모든 클라이언트에게 점수 정보 전송
        foreach (var client in lob.clients)
        {
            SendMessageToClient(client, serializedData);
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
    public void OnQuizAnsweredCorrectly(ServerClient c, string nickname, string answer)
    {
        // 해당 클라이언트가 속한 로비 찾기
        Lobby currentLobby = lobbies.FirstOrDefault(lobby => lobby.clients.Any(client => client.clientName == nickname));

        // 정답 메시지 전송
        string message = $"{nickname}님이 정답을 맞췄습니다! 정답: {answer}";
        Debug.Log(message);
        BroadcastMessage(message);

        // 정답자를 찾아 점수 증가 

        if (c.clientName == nickname)
        {
            c.score += 10; // 점수 10점 추가
            Debug.Log($"{nickname}의 점수: {c.score}");
        }

        BroadcastAllScores(currentLobby); // 전체 클라이언트에게 점수 전송

        // 실행 중인 타이머 중단
        if (quizTimerCoroutine != null)
        {
            StopCoroutine(quizTimerCoroutine);
            quizTimerCoroutine = null; // 중단 후 핸들 초기화
        }

        //BroadcastNextQuiz();
        StartCoroutine(CountdownAndBroadcastQuiz());
    }



    // 시작버튼 누르면 문제 출력되는 함수
    public void OnBroadcastQuizButton()
    {
        //SceneController.Instance.LoadScene("QuizScene");
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
        BroadcastQuiz(nextQuiz.id, nextQuiz.question.Replace(";", ","), nextQuiz.answer);
        Debug.Log($"출제된 퀴즈: {nextQuiz.question}");
        QuizManager.Instance.isStartQuiz = true;

        // 퀴즈 타이머 30초 시작
        quizTimerCoroutine = StartCoroutine(StartQuizTimer(30));
    }

    private Coroutine quizTimerCoroutine; // 타이머 코루틴 핸들 저장 변수
    private IEnumerator StartQuizTimer(int timeLimit)
    {
        int countdownTime = timeLimit; // 타이머 시간

        while (countdownTime > 0)
        {
            BroadcastCountdown(countdownTime);
            Debug.Log($"타이머: {countdownTime}");
            yield return new WaitForSeconds(1); // 1초 대기
            countdownTime--;
        }

        // 타이머가 0이 되면 자동으로 다음 문제로 넘어감
        Debug.Log("타이머 종료! 다음 문제로 넘어갑니다.");

        // 타이머 종료 후 다음 문제로
        StartCoroutine(CountdownAndBroadcastQuiz());
    }

    // 카운트다운 후 퀴즈를 브로드캐스트하는 코루틴
    private IEnumerator CountdownAndBroadcastQuiz()
    {
        yield return new WaitForSeconds(0.5f); // 로딩시간
        QuizManager.Instance.isStartQuiz = false;

        if (quizCount >= 10)
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
