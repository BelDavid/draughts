using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Draughts.Utils;

namespace Draughts.Players.AI
{
    public static class BoardEvaluator
    {
        /* 
         * Maximizing for white, minimaxing for Black
         * Higher fitness is better for white, and lower is better for Black
         */


        public static double Evaluate(BoardState state, BoardEvaluatorType evaluator, RulesType rules)
        {
            if (evaluator == BoardEvaluatorType.Basic)
            {
                return BasicEvalAnyRules(state);
            }
            else
            {
                throw new ArgumentException($"Evaluator {Enum.GetName(typeof(BoardEvaluatorType), evaluator)} not implemented");
            }
        }

        private static double BasicEvalAnyRules(BoardState state)
        {
            double fit = 0f;

            foreach (var (pos, pieceType) in state.IterateBoard())
            {
                if (pieceType != PieceType.None)
                {
                    var f = GetRank(pieceType) == PieceRank.Man  ? 1d
                          : GetRank(pieceType) == PieceRank.King ? 5d
                          : 0d;


                    fit += GetColor(pieceType) == PieceColor.White ?  f
                         : GetColor(pieceType) == PieceColor.Black ? -f
                         : 0d;
                }
            }

            return fit;
        }
    }
}
