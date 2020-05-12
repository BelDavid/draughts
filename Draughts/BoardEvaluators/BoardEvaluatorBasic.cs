﻿using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    public class BoardEvaluatorBasic : IBoardEvaluator
    {
        

        public double Evaluate(BoardState state)
        {
            double fit = 0f;

            foreach (var (pos, pieceType) in state.IterateValidPlacesOnBoard())
            {
                if (pieceType != PieceType.None)
                {
                    var f = Utils.GetRank(pieceType) == PieceRank.Man ? 1d
                          : Utils.GetRank(pieceType) == PieceRank.King ? 5d
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
