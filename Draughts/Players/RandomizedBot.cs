using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Players
{
    class RandomizedBot : Player
    {
        public override Move MakeMove(BoardState boardState)
        {
            var moves = boardState.GetAvaiableMoves();

            return moves != null && moves.Count > 0 ? moves[Utils.rand.Next(moves.Count)] : null;
        }
    }
}