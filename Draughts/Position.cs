using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts
{
    public struct Position
    {
        public byte column, row;

        public Position(byte column, byte row) : this()
        {
            this.column = column;
            this.row = row;
        }

        public static Position None = new Position(255, 255);


        public static Position operator +(Position pos, (int row, int column) delta)
        {
            return new Position { row = (byte)(pos.row + delta.row), column = (byte)(pos.column + delta.column), };
        }

        public static implicit operator Position((byte row, byte column) pos)
        {
            return new Position() { row = pos.row, column = pos.column };
        }
        public static implicit operator (byte, byte)(Position pos)
        {
            return (pos.row, pos.column);
        }

        public override bool Equals(object obj) => obj is Position a ? this == a : false;
        public override int GetHashCode() => (row << 8) | column;

        public static bool operator ==(Position a, Position b) => a.row == b.row && a.column == b.column;
        public static bool operator !=(Position a, Position b) => !(a == b);

    }
}