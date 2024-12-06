using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alpa_QuizManager : MonoBehaviour
{
    public List<Player> players = new List<Player>();       //현재 플레이어 목록
    public int currentRound;                                //현재 라운드
    public int totalRound;                                  //라운드 수
    public List<Quiz> everyQuiz = new List<Quiz>();         //모든 퀴즈
    public List<Quiz> usedQuiz = new List<Quiz>();          //사용된 퀴즈
    public float coolTime;                                  //정답 쿨타임 시간
    public Queue<Player> recevieAnswer;                     //받은 정답 큐

    //라운드 시작
    public void StartRound()
    {
        
    }

    //라운드 종료
    public void EndRound()
    {

    }

    //퀴즈 카운트 다운
    public void CountDown()
    {

    }

    //다음 퀴즈 가져오기
    public void GetNextQuiz()
    {

    }

    //게임 종료, 결과 씬으로 이동 신호 전송
    public void EndGame()
    {

    }
}
