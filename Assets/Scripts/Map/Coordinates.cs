using UnityEngine;
using System.Collections.Generic;
using UFB.Core;

namespace UFB.Map {
    public class Coordinates : IDictionaryConvertable
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2Int Vector => new Vector2Int(X, Y);

        private readonly string[] _tileColumns =  { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K",
                                                "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U",
                                                "V", "W", "X", "Y", "Z" };

        public string ColumnName => _tileColumns[X];
        public string RowName => Y.ToString();
        public string Id { get => $"tile_{ColumnName}_{RowName}"; }
        public string GameId { get => $"tile_{ColumnName}_{Y+1}"; }
        
        public Coordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Coordinates FromVector2Int(Vector2Int vector)
        {
            return new Coordinates(vector.x, vector.y);
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(X, Y);
        }

        public Coordinates[] Adjacent(int minX, int maxX, int minY, int maxY)
        {
            List<Coordinates> adjacent = new List<Coordinates>();
            if (X > minX)
                adjacent.Add(new Coordinates(X - 1, Y));
            if (X < maxX)
                adjacent.Add(new Coordinates(X + 1, Y));
            if (Y > minY)
                adjacent.Add(new Coordinates(X, Y - 1));
            if (Y < maxY)
                adjacent.Add(new Coordinates(X, Y + 1));
            return adjacent.ToArray();

        }

        public bool IsAdjacent(Coordinates other)
        {
            return (other.X == this.X && Mathf.Abs(other.Y - this.Y) == 1) ||
                (other.Y == this.Y && Mathf.Abs(other.X - this.X) == 1);
        }


        public static Coordinates Random(int maxX, int maxY)
        {
            return new Coordinates(
                UnityEngine.Random.Range(0, maxX),
                UnityEngine.Random.Range(0, maxY)
            );
        }

        public Coordinates[] Adjacent() 
        {
            return new Coordinates[] {
                new Coordinates(X, Y + 1),
                new Coordinates(X, Y - 1),
                new Coordinates(X + 1, Y),
                new Coordinates(X - 1, Y)
            };
        }

        public float DistanceTo(Coordinates other)
        {
            // use manhattan distance
            return Mathf.Abs(X - other.X) + Mathf.Abs(Y - other.Y);
            // return Mathf.Sqrt(Mathf.Pow(X - other.X, 2) + Mathf.Pow(Y - other.Y, 2));
        }


        public override bool Equals(object obj)
        {
            if (obj is Coordinates coordinates)
            {
                return X == coordinates.X && Y == coordinates.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X * 17 + Y;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }


        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object> {
                { "x", X },
                { "y", Y }
            };
        }
    }
}