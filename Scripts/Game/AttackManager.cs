using GameClasses;
using Photon.Pun;
using UnityEngine;

namespace Game
{
    public class AttackManager : MonoBehaviour
    {
        public GridManager attackGrid;
        private Cell selectedCell;
        
        [SerializeField] GameManager gm;

        private void Start()
        {
            attackGrid.GenerateGrid(GameClasses.CellType.Attack);
        }

        public void SelectCellForAttack(int x, int y)
        {
            // Сбрасываем состояние предыдущей выбранной клетки
            if (selectedCell != null)
            {
                // Не сбрасываем состояние, если клетка поражена или пропущена
                if (selectedCell.GetState() != CellState.Hit && 
                    selectedCell.GetState() != CellState.Missed)
                {
                    selectedCell.SetState(CellState.Default);
                }
            }

            // Получаем новую клетку
            var cell = attackGrid.GetCell(x, y);
            if (cell == null)
            {
                Debug.LogWarning("Cell is null or invalid."); // Проверяем наличие клетки
                return;
            }

            if (cell.GetComponent<Cell>().GetState() != CellState.Hover)
            {
                Debug.Log("Invalid or already targeted cell selected.");
                return;
            }

            // Устанавливаем новую выбранную клетку
            selectedCell = cell.GetComponent<Cell>();
            Debug.Log("Selected cell: " + selectedCell.Coordinates);
            selectedCell.SetState(CellState.Occupied);
        }


        public Cell GetSelectedCell()
        {
            if (selectedCell == null)
            {
                Debug.LogWarning("No cell is currently selected.");
                return null; // Возвращаем null, если ничего не выбрано
            }

            return selectedCell;
        }
        
        public void SetNullSelectedCell()
        {
            selectedCell = null;
        }

        public void HoverOverCell(int x, int y)
        {
            var cell = attackGrid.GetCell(x, y)?.GetComponent<Cell>();
            if (cell == null) return;

            // Проверяем, чтобы состояние клетки не менялось, если она уже занята, поражена или промахнута
            if (cell.GetState() == CellState.Occupied || 
                cell.GetState() == CellState.Hit || 
                cell.GetState() == CellState.Missed)
            {
                return;
            }

            // Устанавливаем состояние Hover
            cell.SetState(CellState.Hover);
        }

        
        public void ClearPreview(int x, int y)
        {
            var cell = attackGrid.GetCell(x, y)?.GetComponent<Cell>();
            if (cell == null) return;

            // Проверяем, чтобы сброс состояния происходил только для клеток в состоянии Hover
            if (cell.GetState() == CellState.Hover)
            {
                cell.SetState(CellState.Default);
            }
        }
    }
}
