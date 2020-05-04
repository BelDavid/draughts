using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Draughts.Utils;

namespace Draughts.BoardEvaluators
{
    public interface IBoardEvaluator
    {
        /* 
         * Maximizing for white, minimaxing for Black
         * Higher fitness is better for white, and lower is better for Black
         */

        double Evaluate(BoardState state, RulesType rules);
    }
}
