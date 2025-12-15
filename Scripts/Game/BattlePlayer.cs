using System;
using System.Collections.Generic;
using GameClasses;


namespace Game
{
    [Serializable]
    public class BattlePlayer
    {
        public string Name { get; private set; }
        public Field PlacementField { get; set; }
        public Field AttackField { get; set; }
        public bool IsTurn { get; set; }
        public int Score { get; private set; }
        public int ShipsLeft { get; set; }
        public int HitStreak { get; set; } // Количество попаданий подряд
    
        public Dictionary<int, int> shipCounts;
    
        public BattlePlayer(string name)
        {
            Name = name;
            PlacementField = new Field();
            AttackField = new Field();
            ShipsLeft = 10; // 1xL4 + 2xL3 + 3xL2 + 4xL1
            shipCounts = new Dictionary<int, int>()
            {
                [4] = 1,
                [3] = 2,
                [2] = 3,
                [1] = 4
            };
            IsTurn = false;
            Score = 0;
            HitStreak = 0; // Изначально нет попаданий
        }
    
        public void SetScore(int score)
        {
            Score = score;
        }
    
        public void RegisterHit()
        {
            ShipsLeft--;
        }
    
        public int GetShipCount(int length)
        {
            return shipCounts[4 - length];
        }
    
        public bool HasShipsRemaining(Field defenderPlacementField, Field attackerAttackField)
        {
            for (int i = 0; i < defenderPlacementField.grid.GetLength(0); i++)
            {
                for (int j = 0; j < defenderPlacementField.grid.GetLength(1); j++)
                {
                    // Проверяем, есть ли клетка с кораблём, которая ещё не подбита
                    if (defenderPlacementField.grid[i, j] == CellState.Occupied &&
                        attackerAttackField.grid[i, j] != CellState.Hit)
                    {
                        return true; // Корабли остались
                    }
                }
            }
            return false; // Все корабли уничтожены
        }
    }
}
