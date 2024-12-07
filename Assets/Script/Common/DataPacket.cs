using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class DataPacket
{
    public string Type { get; set; }
    public string Value { get; set; }

    // 생성자
    public DataPacket(string type, string value="")
    {
        Type = type;
        Value = value;
    }

    public byte[] Serialize()
    {
        // 문자열을 UTF-8로 바이트 배열로 변환
        string data = $"{Type}|{Value}";
        return Encoding.UTF8.GetBytes(data);
    }

    public static DataPacket Deserialize(byte[] data)
    {
        string rawData = Encoding.UTF8.GetString(data);
        string[] parts = rawData.Split('|');

        if (parts.Length >= 2)
        {
            string type = parts[0];
            string value = string.Join("|", parts.Skip(1)); // Value에 포함된 "|"를 처리하기 위해 나머지 부분을 연결
            return new DataPacket(type, value);
        }

        // 데이터가 잘못된 경우 null 반환 (또는 예외 처리)
        return null;
    }
}
