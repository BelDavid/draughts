using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Draughts.Players
{
    class MinimaxBot : Player
    {
        public MinimaxBot(int depth, ProgressBar progressBar)
        {
            this.depth = depth;
            this.progressBar = progressBar;
        }

        private readonly int depth;
        private readonly ProgressBar progressBar;

        public override Move MakeMove(BoardState boardState)
        {
            return Search(boardState, depth);
        }

        private Move Search(BoardState state, int depth)
        {
            var moves = state.GetAvaiableMoves(Color);

            return moves[Utils.rand.Next(moves.Count)];

            if (depth == 0)
            {

            }
            else
            {
                foreach (var move in moves)
                {
                    
                }
            }
        }
    }
}
