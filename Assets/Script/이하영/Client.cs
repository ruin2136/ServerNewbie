using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Text;
using System;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    public InputField IPInput, PortInput, NickInput;
    string clientName;

    bool socketReady;
    TcpClient socket;
    NetworkStream stream;

    private bool isCooldown = false; // 입력 쿨타임 상태 확인 변수

    public static Client Instance { get; private set; }
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

    public void ConnectToServer()
    {
        if (socketReady) return;

        string ip = string.IsNullOrEmpty(IPInput.text) ? "127.0.0.1" : IPInput.text;
        int port = string.IsNullOrEmpty(PortInput.text) ? 7777 : int.Parse(PortInput.text);

        try
        {
            socket = new TcpClient(ip, port);
            stream = socket.GetStream();
            socketReady = true;

            clientName = string.IsNullOrEmpty(NickInput.text) ? "Guest" + UnityEngine.Random.Range(1000, 10000) : NickInput.text;

            DataPacket nicknamePacket = new DataPacket("nickname", clientName);
            byte[] serializedData = nicknamePacket.Serialize();
            Send(serializedData);

            // 씬 이동 
            SceneController.Instance.LoadScene("LobbyScene"); 

        }
        catch (Exception e)
        {
            //Chat.instance.ShowMessage($"소켓 에러: {e.Message}");
            Debug.LogError("소켓에러");
        }
    }

    void Update()
    {
        if (socketReady && stream.DataAvailable)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessReceivedData(data); // 수신 데이터 처리
            }
        }
    }

    StringBuilder receivedData = new StringBuilder();

    void ProcessReceivedData(string data)
    {
        receivedData.Append(data);

        while (receivedData.ToString().Contains("\n"))
        {
            int delimiterIndex = receivedData.ToString().IndexOf("\n");
            string completePacket = receivedData.ToString(0, delimiterIndex);
            receivedData.Remove(0, delimiterIndex + 1);

            OnIncomingData(completePacket); // 패킷 처리
        }
    }
    
    void OnIncomingData(string data)
    {
        Debug.Log("서버에서 받은 데이터: " + data);

        DataPacket packet = DataPacket.Deserialize(Encoding.UTF8.GetBytes(data));
        Debug.Log($"받은 패킷 타입: {packet.Type}, 값: {packet.Value}");

        // 나중에 시간나면 스위치문으로..
        if (packet.Type == "message" && Chat.instance)
        {
            Chat.instance.ShowMessage(packet.Value);
        }
        else if (packet.Type == "nickname")
        {
            clientName = packet.Value;
            Chat.instance.ShowMessage($"닉네임: {clientName}으로 접속되었습니다.");
        }
        else if (packet.Type == "quiz")
        {
            string[] quizData = packet.Value.Split('|');
            if (quizData.Length == 2 && int.TryParse(quizData[0], out int quizId))
            {
                string question = quizData[1];
                if (!string.IsNullOrEmpty(question))
                {
                    QuizManager.Instance.QuizText.text = question;
                    Debug.Log($"퀴즈 ID={quizId}, Question={question}");
                }
                else
                {
                    Debug.LogError("퀴즈 질문이 빈 값입니다.");
                }
            }
            else
            {
                Debug.LogError("퀴즈 데이터 형식이 잘못되었습니다: " + packet.Value);
            }
        }
        else if (packet.Type == "score") 
        {
            string[] scoreData = packet.Value.Split('|');
            if (scoreData.Length == 2)
            {
                string nickname = scoreData[0];
                if (int.TryParse(scoreData[1], out int score))
                {
                    Debug.Log($"{nickname}의 점수 업데이트: {score}");

                    QuizManager.Instance.scoreText.text=$"{nickname}: {score}";
                }
            }
        }
        else if(packet.Type=="countdown")
        {
             if (int.TryParse(packet.Value, out int countdownTime))
            {
                QuizManager.Instance.CountdownText.text = countdownTime.ToString(); // 클라이언트에서 카운트다운 표시
                //Debug.Log($"서버에서 받은 카운트다운: {countdownTime}");
            }

        }
    }

    void Send(byte[] data)
    {
        if (!socketReady) return;

        try
        {
            stream.Write(data, 0, data.Length);
            stream.Flush();
            Debug.Log("서버로 전송한 데이터: " + Encoding.UTF8.GetString(data));
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"데이터 전송 에러: {e.Message}");
        }
    }

    public void OnSendButton(InputField SendInput)
    {
        if (!socketReady || isCooldown) return; // 쿨타임 중이면 무시
        if (SendInput == null || string.IsNullOrWhiteSpace(SendInput.text)) return;

        string message = SendInput.text.Trim();
        SendInput.text = "";

        DataPacket messagePacket = new DataPacket("message", message);
        byte[] serializedMessage = messagePacket.Serialize();
        Send(serializedMessage);

        // 정답 체크
        if(QuizManager.Instance)
        {
            bool isCorrect = QuizManager.Instance.CheckAnswer(message);
            if (!isCorrect && QuizManager.Instance.isStartQuiz)
            {
                StartCoroutine(InputCooldown(3f)); // 정답이 아니면 쿨타임 3초 적용
            }
        }
        else 
        {
            Debug.Log("퀴즈매니저 널");
        }
       
    }
    private IEnumerator InputCooldown(float cooldownTime)
    {
        isCooldown = true;

        // 입력 비활성화
        Chat.instance.SendInput.interactable = false;
        Chat.instance.SendInput.interactable = false;

        float remainingTime = cooldownTime;
        while (remainingTime > 0)
        {
            // 남은 시간 표시
            Chat.instance.SendInput.text = $"쿨타임: {remainingTime:F1}초";
            yield return new WaitForSeconds(0.1f);
            remainingTime -= 0.1f;
        }

        // 쿨타임 종료 후 상태 복구
        Chat.instance.SendInput.text = ""; // 쿨타임이 끝나면 텍스트 비우기
        Chat.instance.SendInput.interactable = true; // 입력 가능
        Chat.instance.SendInput.interactable = true; // 버튼 활성화
        isCooldown = false;
    }


    void OnApplicationQuit()
    {
        CloseSocket();
    }

    void CloseSocket()
    {
        if (!socketReady) return;

        try
        {
            stream.Close();
            socket.Close();
            socketReady = false;
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"소켓 닫기 에러: {e.Message}");
        }
    }
}
