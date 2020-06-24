using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    public class BoardEvaluatorProgressive : IBoardEvaluator
    {
        public double Evaluate(BoardState state)
        {
            double fit = 0d;

            foreach (var (pos, pieceType) in state.IterateBoard())
            {
                if (pieceType != PieceType.None)
                {
                    var f = 0d;

                    if (Utils.GetRank(pieceType) == PieceRank.Man)
                    {
                        f = 2;
                        var p = 1 / (state.NumberOfRows - 1);
                        
                        f += Utils.GetColor(pieceType) == PieceColor.White ? 1-p
                           : Utils.GetColor(pieceType) == PieceColor.Black ? p
                           : 0d;
                    }
                    else if (Utils.GetRank(pieceType) == PieceRank.King)
                    {
                        f = 5d;
                    }

                    fit += Utils.GetColor(pieceType) == PieceColor.White ? f
                         : Utils.GetColor(pieceType) == PieceColor.Black ? -f
                         : 0d;
                }
            }

            return fit;
        }

        public void Validate(GameRules gameRules)
        {
        }
    }
}
