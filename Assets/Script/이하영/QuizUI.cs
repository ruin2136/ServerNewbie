using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuizUI : MonoBehaviour
{
    public static QuizUI Instance { get; private set; }
    public Text QuizText; // 문제 출력할 Text UI
    public Text CountdownText; // 카운트다운 Text UI

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetQuiz(string question)
    {
        QuizText.text = question;
    }
}
