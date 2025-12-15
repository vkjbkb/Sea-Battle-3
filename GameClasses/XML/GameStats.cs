using System;

public class GameStats
{
    public DateTime Time { get; set; } // Время игры
    public string RoomName { get; set; } // Имя комнаты
    public int Score { get; set; } // Очки
    public bool VictoryStatus { get; set; } // Победа/Поражение

    public override string ToString()
    {
        return $"[Time: {Time:g}, Room: {RoomName}, Score: {Score}, Victory: {VictoryStatus}]";
    }
}