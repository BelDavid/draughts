using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draughts.Pieces;
using Draughts.Rules;
using Draughts.Game;

namespace Draughts.Players
{
    public abstract class Player
    {
        public PieceColor Color { get; private set; } = PieceColor.None;

        protected RulesType rules;
        protected GameControl game;
        public readonly string id;

        public Player(string id) {
            this.id = id ?? string.Empty;
        }

        public void Setup(PieceColor color, RulesType rules, GameControl game)
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
            this.game = game;
        }



        /// <summary>
        /// Returns null if no move avaiable (player loses)
        /// </summary>
        /// <returns></returns>
        public abstract Move MakeMove(BoardState boardState);

    }
}