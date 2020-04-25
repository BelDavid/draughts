using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draughts.Pieces;
using Draughts.Rules;

namespace Draughts.Players
{
    public abstract class Player
    {
        public PieceColor Color { get; private set; } = PieceColor.None;

        public RulesType rules { get; private set; }

        public Player() { }

        public void Setup(PieceColor color, RulesType rules)
        {
            if (Color != PieceColor.None)
            {
                throw new Exception("Already set up");
            }
            if (color == PieceColor.None)
            {
                throw new Exception("Invalid color");
            }

            Color = color;
            this.rules = rules;
        }



        /// <summary>
        /// Returns null if no move avaiable (player loses)
        /// </summary>
        /// <returns></returns>
        public abstract Move MakeMove(BoardState boardState);

    }
}