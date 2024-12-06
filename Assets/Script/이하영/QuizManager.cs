using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance { get; private set; }

    [Header("퀴즈 CSV 파일 이름 (Resources 폴더 내)")]
    public string csvFileName = "quiz";

    private List<Quiz> quizzes = new List<Quiz>(); // 파싱된 퀴즈 목록
    private HashSet<int> usedQuizIds = new HashSet<int>(); // 이미 사용된 퀴즈 ID를 저장
    private Quiz currentQuiz; // 현재 퀴즈

    public Text QuizText; // 문제 출력할 Text UI
    public Text CountdownText; // 카운트다운 Text UI
    public Text scoreText; // 점수 Text UI
    public bool isStartQuiz; // 퀴즈 중이면 true, 아니면 false

      public Dictionary<string, int> clientScores = new Dictionary<string, int>(); // 클라이언트 점수 저장용 딕셔너리

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
        Initialization();
        LoadQuizzes();
    }

    // 초기화
    public void Initialization()
    {
        isStartQuiz = false;
        QuizText.text = "";
        CountdownText.text = "";
        scoreText.text = "";
    }

    // 퀴즈 데이터를 CSV에서 로드하고 파싱합니다.
    public void LoadQuizzes()
    {
        quizzes = ParseQuizzesFromCSV(csvFileName);

        if (quizzes.Count > 0)
        {
            Debug.Log($"총 {quizzes.Count}개의 퀴즈를 성공적으로 로드했습니다.");
        }
        else
        {
            Debug.LogError("퀴즈를 로드하지 못했습니다. CSV 파일을 확인하세요.");
        }
    }

    
    // CSV 파일에서 퀴즈 데이터 파싱
    private List<Quiz> ParseQuizzesFromCSV(string fileName)
    {
        List<Quiz> quizList = new List<Quiz>();

        // Resources 폴더에서 CSV 파일 읽기
        TextAsset csvData = Resources.Load<TextAsset>(fileName);
        if (csvData == null)
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {fileName}");
            return quizList;
        }

        // 줄 단위로 데이터 분리
        string[] rows = csvData.text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 첫 줄은 헤더, 데이터는 두 번째 줄부터 시작
        for (int i = 1; i < rows.Length; i++)
        {

            string[] columns = rows[i].Split(',');

            try
            {
                Quiz quiz = new Quiz
                {
                    id = int.Parse(columns[0].Trim()), // ID
                    question = columns[1].Trim(), // 질문
                    answer = columns[2].Trim() // 정답
                };

                quizList.Add(quiz);
            }
            catch (Exception ex)
            {
                Debug.LogError($"CSV 파싱 오류 (줄 {i + 1}): {ex.Message}");
            }
        }

        Debug.Log("퀴즈 CSV 파싱 완료!");
        return quizList;
    }

    
    // 랜덤으로 퀴즈를 선택하고 반환
    public Quiz GetRandomQuiz()
    {
        if (quizzes.Count == 0)
        {
            Debug.LogWarning("퀴즈 목록이 비어 있습니다.");
            return null;
        }

        Quiz randomQuiz;
        int attempts = 0; // 무한 루프 방지를 위한 시도 횟수

        do
        {
            randomQuiz = quizzes[UnityEngine.Random.Range(0, quizzes.Count)];
            attempts++;
        } while (usedQuizIds.Contains(randomQuiz.id) && attempts < quizzes.Count);

        if (attempts >= quizzes.Count)
        {
            Debug.LogWarning("모든 퀴즈가 출제되었습니다.");
            return null; // 모든 퀴즈가 사용된 경우
        }

        usedQuizIds.Add(randomQuiz.id);
        currentQuiz = randomQuiz; // 현재 퀴즈 설정
        return randomQuiz;
    }


    // 현재 퀴즈의 정답 여부 확인
    public bool CheckAnswer(string userAnswer)
    {
        if (currentQuiz == null)
        {
            Debug.LogWarning("현재 퀴즈가 설정되지 않았습니다.");
            return false;
        }

        string cleanedAnswer = CleanString(currentQuiz.answer);
        string cleanedUserAnswer = CleanString(userAnswer);

        return string.Equals(cleanedAnswer, cleanedUserAnswer, StringComparison.OrdinalIgnoreCase);
    }

    
    // 문자열 정리. (제어 문자 제거 및 공백 트림)
    private string CleanString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return new string(input.Where(c => !char.IsControl(c)).ToArray()).Trim();
    }
}
