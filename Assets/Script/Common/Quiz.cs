public class Quiz
{
    public string proglem;
    public string correctAnswer;
    public int quizScore;
    
    //정답 확인 메서드
    public bool CheckCorrect(string answer)
    {
        if(correctAnswer.Equals(answer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
