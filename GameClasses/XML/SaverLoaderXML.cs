using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public static class SaverLoaderXML
{
    private static string GetFilePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    // Сохранение всех сессий
    public static void SaveGameSessions(GameStatsCollection collection, string fileName = "GameSessions.xml")
    {
        string filePath = GetFilePath(fileName);

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GameStatsCollection));
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(stream, collection);
            }
            Debug.Log($"Game sessions saved to {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save game sessions: {ex.Message}");
        }
    }

    // Загрузка всех сессий
    public static GameStatsCollection LoadGameSessions(string fileName = "GameSessions.xml")
    {
        string filePath = GetFilePath(fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Save file not found, returning an empty collection.");
            return new GameStatsCollection(); // Возвращаем пустую коллекцию
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GameStatsCollection));
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                GameStatsCollection collection = (GameStatsCollection)serializer.Deserialize(stream);
                Debug.Log($"Game sessions loaded from {filePath}");
                return collection;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load game sessions: {ex.Message}");
            return new GameStatsCollection(); // Возвращаем пустую коллекцию
        }
    }

    // Добавление одной сессии в существующий файл
    public static void AddGameSession(GameStats stats, string fileName = "GameSessions.xml")
    {
        var collection = LoadGameSessions(fileName);
        collection.Sessions.Add(stats);
        SaveGameSessions(collection, fileName);
    }
}
