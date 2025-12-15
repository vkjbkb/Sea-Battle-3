using UnityEngine;

namespace GameClasses
{
    public class Field
    {
    
        public CellState[,] grid; // Игровая сетка (10x10)
        public int size = 10; // Размер сетки
    
        public Field()
        {
            grid = new CellState[size, size];
    
            // Инициализация поля состояниями Default
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    grid[i, j] = CellState.Default;
                }
            }
        }
    
        public CellState GetCellState(int x, int y)
        {
            return grid[x, y];
        }
    
        public void SetCellState(int x, int y, CellState state)
        {
            grid[x, y] = state;
        }
        
        public void SetCellStateMatrix(CellState[,] newStates)
        {
            grid = newStates;
        }
        
        private void PrintField(CellState[,] matrix)
        {
            int size = matrix.GetLength(0);
            
            for (int y = 0; y < size; y++)
            {
                string row = "";
                for (int x = 0; x < size; x++)
                {
                    row += $"{(int)matrix[y, x]} "; // Преобразуем CellState в целое число для упрощения вывода
                }
                Debug.Log(row);
            }
        }
        
        public bool CheckVictory()
        {
            foreach (var cell in grid)
            {
                if (cell == CellState.Occupied) return false; // Если есть незатопленный корабль
            }
            return true; // Все корабли потоплены
        }
    }
}

