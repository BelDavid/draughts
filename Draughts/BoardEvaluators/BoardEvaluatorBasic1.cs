using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    public class BoardEvaluatorBasic1 : IBoardEvaluator
    {
        public double weightMan = 1d;
        public double weightKing = 10d;

        public double Evaluate(BoardState state)
        {
            double fit = 0d;

            foreach (var (_, pieceType) in state.IterateBoard())
            {
                if (pieceType != PieceType.None)
                {
                    var f = Utils.GetRank(pieceType) == PieceRank.Man ? weightMan
                          : Utils.GetRank(pieceType) == PieceRank.King ? weightKing
                          : 0d;


                    fit += Utils.GetColor(pieceType) == PieceColor.White ? f
                         : Utils.GetColor(pieceType) == PieceColor.Black ? -f
                         : 0d;
                }
            }

            return fit;
        }

        public void Validate(GameRules rules)
        {

        }
    }
}
