using System;
using System.Collections;
using System.Collections.Generic;
using GameClasses;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GameManager : MonoBehaviourPunCallbacks
{
    public BattlePlayer Player1 { get; set; }
    public BattlePlayer Player2 { get; set; }
    
    public bool player1Ready = false;
    public bool player2Ready = false;
    
    public bool player1Turn = false;
    public bool player2Turn = false;
    
    private bool IsWaitingForRPC = false;
    
    public bool isEndGame = false;

    [SerializeField] private TMP_Text timeDisplay; // Отображение времени
    private const double TurnTimeLimitSeconds = 60.0; // В секундах
    private double turnEndTime; // Время завершения текущего хода
    private Coroutine timerCoroutine; // Ссылка на корутину
    
    private PhotonView pView;
    
    [SerializeField] private GameMenuConroller menuController;
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private ShipPlacementManager shipPlacementManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button endTurnButton;

    private void Awake()
    {
        pView = GetComponent<PhotonView>();
        endTurnButton.onClick.AddListener(OnEndTurnButtonClick);
        
        Player1 = new BattlePlayer("MasterPlayer");
        Player2 = new BattlePlayer("GuestPlayer");
    }

    public void UpdateLocalPlayerTurns(bool player1isTurn, bool player2isTurn)
    {
        player1Turn = player1isTurn;
        player2Turn = player2isTurn;
    }

    private void StartGame()
    {
        Debug.Log("Game has started!");
    
        Player1.IsTurn = true;
        Player2.IsTurn = false;
        UpdateLocalPlayerTurns(Player1.IsTurn, Player2.IsTurn);
    
        UpdateEndTurnButtonState();
        UpdateScoreText(); // Отображаем начальные очки
        StartTurnTimer(); // Запуск таймера
    }
    
    private void EndGame(int winnerIndex)
    {
        isEndGame = true;
        
        FindObjectOfType<ShipAnalytics>().SetEmptyStatusString();
        
        string winnerName = winnerIndex == 1 ? Player1.Name : Player2.Name;
        Debug.Log($"Player {winnerName} wins!");

        StopTurnTimer(); // Останавливаем таймер

        // Определяем результат для текущего игрока
        bool isWinner = (PhotonNetwork.IsMasterClient && winnerIndex == 1) || 
                        (!PhotonNetwork.IsMasterClient && winnerIndex == 2);

        int playerScore = isWinner ? Player1.Score : Player2.Score;
        
        if (Player1.IsTurn == false && Player2.IsTurn == false) 
        {
            // Если один из игроков покидает комнату после завершения игры, не вызываем EndGame снова
            return;
        }
        
        // Сохраняем результаты игры
        SaveGameStats(isWinner);

        if (isWinner)
        {
            menuController.WinGame(int.Parse(scoreText.text)); // Вызываем WinGame
        }
        else
        {
            menuController.LoseGame(int.Parse(scoreText.text)); // Вызываем LoseGame
        }
        
        CloseRoom();
    }
    private void CloseRoom()
    {
        // Если этот игрок является MasterClient, то закрываем комнату
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;  // Закрываем комнату
            PhotonNetwork.CurrentRoom.IsVisible = false; // Отключаем видимость комнаты
            Debug.Log("Room is now closed and invisible.");
        }
    }
    
    // Handling when an opponent leaves the room
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!isEndGame)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Opponent left the room. You win!");
                EndGame(1);
            }
            else
            {
                Debug.Log("Opponent left the room. You win!");
                EndGame(2);
            }
        }
    }

    private void SaveGameStats(bool isWinner)
    {
        var gameStats = new GameStats
        {
            Score = int.Parse(scoreText.text),
            Time = DateTime.Now, // Используем текущее время
            RoomName = PhotonNetwork.CurrentRoom.Name,
            VictoryStatus = isWinner
        };

        SaverLoaderXML.AddGameSession(gameStats);
        Debug.Log("Game session saved successfully.");
    }


    [PunRPC]
    public void SetPlayerReady(int playerIndex, CellState[] serializedMatrix)
    {
        CellState[,] matrix = DeserializeMatrix(serializedMatrix, 10, 10);

        if (!PhotonNetwork.IsMasterClient && playerIndex == 1)
        {
            Player1.PlacementField.SetCellStateMatrix(matrix);
            player1Ready = true;
            Debug.Log($"Player 1 ({Player1.Name}) Placement Field:");
            PrintPlacementField(Player1);
        }
        else if (PhotonNetwork.IsMasterClient && playerIndex == 2)
        {
            Player2.PlacementField.SetCellStateMatrix(matrix);
            player2Ready = true;
            Debug.Log($"Player 2 ({Player2.Name}) Placement Field:");
            PrintPlacementField(Player2);
        }
        
        // Уведомляем ShipAnalytics об изменении статуса текста
        FindObjectOfType<ShipAnalytics>().UpdateStatusText();
        
        CheckBothPlayersReady();
    }
    
    private void CheckBothPlayersReady()
    {
        if (player1Ready && player2Ready)
        {
            Debug.Log("Both players are ready. Starting the game!");
            readyButton.gameObject.SetActive(false);
            endTurnButton.gameObject.SetActive(true);
            StartGame();

            FindObjectOfType<ShipAnalytics>().UpdateStatusText(); // Убираем текст "Разместите корабли"
        }
    }


    private void OnEndTurnButtonClick()
    {
        Debug.Log("Attempting to end turn.");

        if (IsWaitingForRPC)
        {
            Debug.LogWarning("Action is temporarily locked! Cannot end turn.");
            return;
        }

        var selectedCell = attackManager.GetSelectedCell();
        if (selectedCell == null)
        {
            Debug.LogWarning("No cell selected for the attack.");
            return;
        }

        Debug.Log($"Ending turn. Player {(Player1.IsTurn ? "1" : "2")} attacks at {selectedCell.Coordinates}.");
        Vector2Int coords = selectedCell.Coordinates;

        IsWaitingForRPC = true;
        pView.RPC("ValidateAttack", RpcTarget.All, coords.x, coords.y, Player1.IsTurn ? 1 : 2);
        attackManager.SetNullSelectedCell();
    }
    
    [PunRPC]
    public void ValidateAttack(int x, int y, int attackerIndex)
    {
        bool isAttacker = (attackerIndex == 1 && PhotonNetwork.IsMasterClient) ||
                          (attackerIndex == 2 && !PhotonNetwork.IsMasterClient);

        BattlePlayer defender = attackerIndex == 1 ? Player2 : Player1;
        BattlePlayer attacker = attackerIndex == 1 ? Player1 : Player2;

        CellState cellState = defender.PlacementField.GetCellState(x, y);
        Debug.Log($"Cell state at ({x}, {y}) is {cellState}.");

        if (cellState == CellState.Occupied)
        {
            Debug.Log($"Player {attackerIndex} hit at ({x}, {y}).");

            // Обновляем поля атакующего
            if (isAttacker)
            {
                attacker.AttackField.SetCellState(x, y, CellState.Hit);
                attackManager.attackGrid.SetStateGridCell(x, y, CellState.Hit);
                
                // Обновление очков с учетом бонуса
                attacker.HitStreak++;
                int bonusMultiplier = attacker.HitStreak;
                attacker.SetScore(attacker.Score + 10 * bonusMultiplier); // 10 очков за попадание, умноженное на текущий бонус
                UpdateScoreText();
            }

            if (!isAttacker)
            {
                // Обновляем поле защитника (PlacementField)
                defender.PlacementField.SetCellState(x, y, CellState.Hit);
                shipPlacementManager.placementGrid.SetStateGridCell(x, y, CellState.Invalid);
            }

            // Проверяем оставшиеся корабли защитника
            if (!defender.HasShipsRemaining(defender.PlacementField, attacker.AttackField))
            {
                Debug.Log($"Player {attackerIndex} wins!");
                IsWaitingForRPC = false;
                EndGame(Player1.IsTurn ? 1 : 2);
                return;
            }

            // Игрок продолжает свой ход после попадания
            ContinueTurn();
        }
        else
        {
            Debug.Log($"Player {attackerIndex} missed at ({x}, {y}).");

            // Обновляем поля атакующего
            if (isAttacker)
            {
                attacker.AttackField.SetCellState(x, y, CellState.Missed);
                attackManager.attackGrid.SetStateGridCell(x, y, CellState.Missed);
            }

            if (!isAttacker)
            {
                // Обновляем поле защитника (PlacementField)
                defender.PlacementField.SetCellState(x, y, CellState.Missed);
                shipPlacementManager.placementGrid.SetStateGridCell(x, y, CellState.Missed);
            }

            // Сбрасываем серию попаданий
            attacker.HitStreak = 0;
            UpdateScoreText();

            // Ход передается другому игроку
            if (isAttacker)
                pView.RPC("SwitchTurns", RpcTarget.All);
        }
        
        IsWaitingForRPC = false;
    }
    
    private void ContinueTurn()
    {
        StartTurnTimer(); // Сброс таймера для текущего игрока
        UpdateEndTurnButtonState();
    }

    [PunRPC]
    private void SwitchTurns()
    {
        Debug.Log("Switching turns.");

        Player1.IsTurn = !Player1.IsTurn;
        Player2.IsTurn = !Player2.IsTurn;
        UpdateLocalPlayerTurns(Player1.IsTurn, Player2.IsTurn);

        UpdateEndTurnButtonState();
        StartTurnTimer();

        FindObjectOfType<ShipAnalytics>().UpdateStatusText(); // Обновляем текст статуса
    }


    private void UpdateEndTurnButtonState()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetEndTurnButtonState(Player1.IsTurn);
        }
        else
        {
            SetEndTurnButtonState(Player2.IsTurn);
        }
    }
    
    private void UpdateScoreText()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            scoreText.text = $"{Player1.Score}";
        }
        else
        {
            scoreText.text = $"{Player2.Score}";
        }
    }

    private void SetEndTurnButtonState(bool isInteractable)
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = isInteractable;
        }
    }

    private void StartTurnTimer()
    {
        turnEndTime = PhotonNetwork.Time + TurnTimeLimitSeconds;
        pView.RPC("SyncTurnEndTime", RpcTarget.All, turnEndTime);
    }

    private void StopTurnTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    [PunRPC]
    private void SyncTurnEndTime(double endTime)
    {
        turnEndTime = endTime;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(UpdateTimeDisplayRoutine());
    }

    private IEnumerator UpdateTimeDisplayRoutine()
    {
        while (true)
        {
            double timeRemaining = turnEndTime - PhotonNetwork.Time;

            if (timeRemaining <= 0)
            {
                timeDisplay.text = "0";
                break;
            }

            timeDisplay.text = Mathf.CeilToInt((float)timeRemaining).ToString();
            yield return new WaitForSeconds(0.1f);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            pView.RPC("SwitchTurns", RpcTarget.All);
        }
    }

    private CellState[,] DeserializeMatrix(CellState[] serializedMatrix, int rows, int cols)
    {
        CellState[,] matrix = new CellState[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                matrix[i, j] = serializedMatrix[i * cols + j];
            }
        }

        return matrix;
    }
    
    private void PrintPlacementField(BattlePlayer player)
    {
        int size = player.PlacementField.grid.GetLength(0);
        string output = $"{player.Name}'s Placement Field:\n";

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                output += $"{(int)player.PlacementField.grid[x, y]} ";
            }
            output += "\n"; // Переход на новую строку после каждой строки матрицы
        }

        Debug.Log(output);
    }
    
    private void PrintAttackField(BattlePlayer player)
    {
        if (player.AttackField == null)
        {
            Debug.LogError($"AttackField of {player.Name} is null.");
            return;
        }

        int size = player.AttackField.grid.GetLength(0);
        string output = $"{player.Name}'s Attack Field:\n";

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                output += $"{(int)player.AttackField.grid[x, y]} ";
            }
            output += "\n"; // Переход на новую строку после каждой строки матрицы
        }

        Debug.Log(output);
    }
}
}

