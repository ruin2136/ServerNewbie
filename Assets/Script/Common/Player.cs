public class Player
{
    // 인터페이스의 속성 구현
    public string name;
    public bool onCoolTime;
    public string answer;
    
    public int score;     //플레이어 점수

    public Player(string nickname)
    {
        name = nickname;
        onCoolTime = false;
        answer = string.Empty;
    }
}
