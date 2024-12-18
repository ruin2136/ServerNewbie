﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Text;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class Client : MonoBehaviour
{
    public InputField IPInput, PortInput, NickInput;
    string clientName;

    bool socketReady;
    TcpClient socket;
    NetworkStream stream;

    [Header("로비 UI 매니저")]
    public LobbyUIManager lobManager;
    public List<string> playerNames = new List<string>();
    [HideInInspector]
    public bool readyBtnSet;
    public List<int> playerScores = new List<int>();

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
            //SceneController.Instance.LoadScene("LobbyScene"); 

        }
        catch (Exception e)
        {
            //Chat.instance.ShowMessage($"소켓 에러: {e.Message}");
            Debug.LogError($"소켓 에러: {e.Message}");
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
        else if (packet.Type == "moveToLobby")
        {
            playerNames = packet.Value.Split(", ").ToList();

            //로비 씬으로 이동
            SceneController.Instance.LoadScene("LobbyScene");
        }
        else if (packet.Type == "moveToQuiz")
        {
            playerNames.Clear();
            playerScores.Clear();

            string[] playerData = packet.Value.Split(';');

            foreach (string player in playerData)
            {
                string[] details = player.Split(',');

                if (details.Length == 2)
                {
                    playerNames.Add(details[0]); // 이름 추가
                    if (int.TryParse(details[1], out int score))
                    {
                        playerScores.Add(score); // 점수 추가
                    }
                    else
                    {
                        playerScores.Add(0); // 기본값으로 0 설정
                    }
                }
            }

            // 퀴즈 씬으로 이동 후, 0.5초 뒤에 점수 업데이트
            StartCoroutine(LoadSceneAndUpdateScore());
        }
        else if (packet.Type == "listUpdate")
        {
            List<string> nameList = packet.Value.Split(", ").ToList();

            // 로비/퀴즈 구분
            if (lobManager != null)
            {
                //로비 업데이트
                lobManager.LobbyUIUpdate(nameList);
            }
            //아니면 퀴즈 업데이트
        }
        else if (packet.Type == "readyBtn")
        {
            //로비 준비 버튼 활성화 비활성화

            if (bool.TryParse(packet.Value, out readyBtnSet))
            {
                Debug.Log($"신규 접속 플레이어 : 준비 상태 변경 to {readyBtnSet}");

                // 신규 접속자는 씬 이동 후 처리
                if (lobManager == null)
                    return;

                if (readyBtnSet)
                {
                    // 준비 버튼 활성화
                    lobManager.SetBtn(readyBtnSet);
                }
                else
                {
                    // 준비 버튼 비활성화
                    lobManager.SetBtn(readyBtnSet);
                    isReady = false;
                }
            }
            else
            {
                Debug.LogError($"Invalid bool value: {packet.Value}");
            }
        }
        else if (packet.Type == "quiz")
        {
            string[] quizData = packet.Value.Split('|');
            if (quizData.Length == 3 && int.TryParse(quizData[0], out int quizId))
            {
                string question = quizData[1];
                string answer = quizData[2];

                Quiz newQuiz = new Quiz { id = quizId, question = question, answer = answer };
                QuizManager.Instance.SetCurrentQuiz(newQuiz);
                QuizManager.Instance.isStartQuiz = true;

                if (!string.IsNullOrEmpty(question))
                {
                    //QuizManager.Instance.QuizText.text = question;
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
            // 점수 데이터 (예: "ㅇㄹ,10;f,0")
            string[] playerData = packet.Value.Split(';'); // ';'로 각 플레이어의 데이터 분리

            foreach (string player in playerData)
            {
                string[] details = player.Split(','); // ','로 이름과 점수를 분리

                if (details.Length == 2)
                {
                    string playerName = details[0];  // 플레이어 이름
                    if (int.TryParse(details[1], out int score))  // 점수 값 파싱
                    {
                        //Debug.Log($"플레이어 이름: {playerName}, 점수: {score}");

                        // 플레이어 이름과 점수를 찾아 업데이트
                        int playerIndex = playerNames.IndexOf(playerName);
                        if (playerIndex >= 0)
                        {
                            // 해당 플레이어의 점수 갱신
                            playerScores[playerIndex] = score;

                            // UI 업데이트
                            QuizManager.Instance.UpdateScoreDisplay(playerNames, playerScores);
                        }
                    }
                }
            }
        }
        else if (packet.Type == "countdown")
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
        if (!socketReady) return; // 쿨타임 중이면 무시
        if (SendInput == null || string.IsNullOrWhiteSpace(SendInput.text)) return;

        string message = SendInput.text.Trim();
        SendInput.text = "";

        DataPacket messagePacket = new DataPacket("message", message);
        byte[] serializedMessage = messagePacket.Serialize();
        Send(serializedMessage);

        // 정답 체크
        if (QuizManager.Instance)
        {
            bool isCorrect = QuizManager.Instance.CheckAnswer(message);
            Debug.Log(isCorrect);
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
    }

    // 씬 로드 후 점수 텍스트 업데이트를 위한 코루틴
    private IEnumerator LoadSceneAndUpdateScore()
    {
        SceneController.Instance.LoadScene("QuizScene");

        // 씬 로드 후 0.5초 대기
        yield return new WaitForSeconds(0.5f);

        // QuizManager의 UpdateScoreDisplay를 호출하여 점수 출력
        QuizManager.Instance.UpdateScoreDisplay(playerNames, playerScores);
    }


    void OnApplicationQuit()
    {
        CloseSocket();
    }

    public void CloseSocket()
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

    #region 추가된 코드(by 전영준)
    public bool isReady = false;

    public void SetReady()
    {
        isReady = !isReady;

        string message = isReady.ToString();

        Debug.LogFormat($"준비 상태 전송 : {message}");
        DataPacket messagePacket = new DataPacket("ready", message);
        byte[] serializedMessage = messagePacket.Serialize();
        Send(serializedMessage);
    }
    #endregion
}
