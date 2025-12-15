namespace GameClasses
{
    public enum CellState
    {
        Default,   // Свободная клетка
        Available,   // Предпросмотр корабля
        Occupied,  // Клетка занята кораблем
        Invalid,   // Недопустимое размещение
        Hit,       // Попадание
        Missed, // Промах
        Hover, // Курсор над клеткой
    }
}
