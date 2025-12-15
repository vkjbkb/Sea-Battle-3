using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LogCollector : MonoBehaviour
{
    public static LogCollector Instance { get; private set; }  // Экземпляр класса, реализующий паттерн Singleton

    private List<string> logMessages = new List<string>();  // Список для хранения логов
    private string logFilePath;  // Путь к файлу, в который будут сохраняться логи

    // Метод для создания синглтона и предотвращения создания лишних экземпляров
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);  // Уничтожаем объект, если экземпляр уже существует
        }
        else
        {
            Instance = this;  // Если экземпляра нет, сохраняем ссылку на текущий
            DontDestroyOnLoad(gameObject);  // Объект не будет уничтожен при смене сцен
        }
    }

    // Метод, который вызывается при старте игры, здесь инициализируется путь для хранения логов
    private void Start()
    {
        // Путь для файла логов, используем persistentDataPath для сохранения на устройстве
        logFilePath = Path.Combine(Application.persistentDataPath, "UnityLogs.txt");

        // Подписываемся на событие логирования Unity
        Application.logMessageReceived += HandleLogMessage;
    }

    // Метод вызывается при выходе из приложения
    private void OnApplicationQuit()
    {
        SaveLogsToFile();  // Сохраняем логи в файл при выходе
    }

    // Обработчик событий логов
    private void HandleLogMessage(string logString, string stackTrace, LogType type)
    {
        // Форматируем сообщение с добавлением времени и типа лога
        string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {logString}";
        
        // Если это ошибка или исключение, добавляем информацию о стеке вызовов
        if (type == LogType.Error || type == LogType.Exception)
            formattedMessage += $"\nStack Trace:\n{stackTrace}";
        
        // Добавляем сформированное сообщение в список логов
        logMessages.Add(formattedMessage);
    }

    // Метод для сохранения собранных логов в файл
    private void SaveLogsToFile()
    {
        try
        {
            // Пишем все сообщения лога в файл
            File.WriteAllLines(logFilePath, logMessages);
            Debug.Log($"Logs saved to {logFilePath}");
        }
        catch (Exception e)
        {
            // Если не удалось сохранить логи, выводим ошибку
            Debug.LogError($"Failed to save logs to file: {e.Message}");
        }
    }
}
