using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;

public class Rank : MonoBehaviour
{
    public Text rankDisplay; // 랭킹을 표시할 UI 텍스트

    public void DisplayRankings(List<int> scores, List<string> names)
    {
        if (scores == null || scores.Count == 0 || names == null || names.Count == 0 || scores.Count != names.Count)
        {
            Debug.LogWarning("[Rank] 플레이어 데이터가 잘못되었습니다.");
            rankDisplay.text = "플레이어 데이터가 없습니다.";
            return;
        }

        // 점수와 이름을 Tuple로 묶고, 점수 내림차순 정렬
        var sortedRankings = scores
            .Select((score, index) => new { Score = score, Name = names[index] })
            .OrderByDescending(entry => entry.Score)
            .ToList();

        // 랭킹 문자열 생성
        StringBuilder rankings = new StringBuilder();
        rankings.AppendLine("현재 랭킹:");

        foreach (var entry in sortedRankings)
        {
            rankings.AppendLine($"{entry.Name}: {entry.Score}점");
        }

        // UI 텍스트에 랭킹 표시
        rankDisplay.text = rankings.ToString();
    }
}
