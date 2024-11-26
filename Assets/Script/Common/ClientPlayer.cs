public class ClientPlayer : Player
{
    public ClientPlayer(string nickname) : base(nickname)
    {
        name = nickname;
        onCoolTime = false;
        answer = string.Empty;
    }

    //정답 서버에 제출
    public void SubmitAnswer(string answer)
    {

    }
    
    //정답 제출 시 쿨타임 부여
    public void BanSubmit()
    {

    }

    //현재 점수를 서버에서 가져와 출력
    public void PrintScore()
    {

    }

    //쿨타임 작동 메서드(비동기)
    public void CoolTime()
    {

    }
}
