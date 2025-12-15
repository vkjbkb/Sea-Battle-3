using System;

public static class ShipFactory
{
    public static Ship CreateShip(string shipType)
    {
        switch (shipType)
        {
            case "Battleship":
                return new Battleship();
            case "Cruiser":
                return new Cruiser();
            case "Destroyer":
                return new Destroyer();
            case "Submarine":
                return new Submarine();
            default:
                throw new ArgumentException("Unknown ship type");
        }
    }
}