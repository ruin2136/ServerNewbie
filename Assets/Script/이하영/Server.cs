using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public InputField PortInput;
    private List<ServerClient> clients;
    private List<ServerClient> disconnectList;

    private TcpListener server;
    private bool serverStarted;

    private Quiz currentQuiz;

    private void Start()
    {
    }

    public void ServerCreate()
    {
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();

        try
        {
            int port = string.IsNullOrEmpty(PortInput.text) ? 7777 : int.Parse(PortInput.text);
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;

            Chat.instance.ShowMessage($"서버가 {port}에서 시작되었습니다.");
            Debug.Log($"서버가 {port}에서 시작되었습니다.");
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"소켓 에러: {e.Message}");
        }
    }

    private void Update()
    {
        if (!serverStarted) return;

        foreach (var c in clients)
        {
            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
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
                    OnIncomingData(c, data);
                }
            }
        }

        for (int i = 0; i < disconnectList.Count; i++)
        {
            BroadcastMessage($"{disconnectList[i].clientName} 연결이 끊어졌습니다.");
            clients.Remove(disconnectList[i]);
            disconnectList.RemoveAt(i);
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

    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        ServerClient newClient = new ServerClient(listener.EndAcceptTcpClient(ar));

        Debug.Log($"새 클라이언트 접속: {newClient.clientName}");

        clients.Add(newClient);
        StartListening();
    }

    private void OnIncomingData(ServerClient c, string data)
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
    }

    private void Broadcast(byte[] data)
    {
        foreach (var c in clients)
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
    }

    // 서버에서 메시지 전송
    public new void BroadcastMessage(string message)
    {
        DataPacket messagePacket = new DataPacket("message", message);
        byte[] serializedMessage = messagePacket.Serialize();
        Broadcast(serializedMessage);
    }

    // 서버에서 퀴즈 전송
    public void BroadcastQuiz(int quizId, string question)
    {
        string quizData = $"{quizId}|{question}";
        DataPacket quizPacket = new DataPacket("quiz", quizData);
        byte[] serializedQuiz = quizPacket.Serialize();
        Broadcast(serializedQuiz);
    }

    // 정답 맞춘 경우 처리 및 새 퀴즈 전송
    public void OnQuizAnsweredCorrectly(string nickname, string answer)
    {
        // 정답 메시지 전송
        string message = $"{nickname}님이 정답을 맞췄습니다! 정답: {answer}";
        Debug.Log(message);
        BroadcastMessage(message);

        //BroadcastNextQuiz();
        StartCoroutine(CountdownAndBroadcastQuiz());
    }


    
    // 시작버튼 누르면 문제 출력되는 함수
    public void OnBroadcastQuizButton()
    {
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
        BroadcastQuiz(nextQuiz.id, nextQuiz.question.Replace(";", ","));
        Debug.Log($"출제된 퀴즈: {nextQuiz.question}");
    }

    // 카운트다운 후 퀴즈를 브로드캐스트하는 코루틴
    private IEnumerator CountdownAndBroadcastQuiz()
    {
        int countdownTime = 3; // 3초 카운트다운
        while (countdownTime > 0)
        {
            QuizManager.Instance.CountdownText.text = countdownTime.ToString(); // UI 업데이트
            Debug.Log($"카운트다운: {countdownTime}");
            yield return new WaitForSeconds(1); // 1초 대기
            countdownTime--;
        }

        QuizManager.Instance.CountdownText.text = ""; // 카운트다운 종료 후 텍스트 제거
        BroadcastNextQuiz(); // 퀴즈 브로드캐스트
    }
}

public class ServerClient
{
    public TcpClient tcp;
    public string clientName;

    public ServerClient(TcpClient clientSocket)
    {
        clientName = "Guest";
        tcp = clientSocket;
    }
}
