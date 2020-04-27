using Draughts.BoardEvaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Draughts.Players
{
    public delegate Player PlayerFactory();

    public static class PlayerFactories
    {
        public static PlayerFactory UserFactory() => () => new User();
        public static PlayerFactory RandomizedBotFactory() => () => new RandomizedBot();
        public static PlayerFactory MinimaxBotFactory(int depth, IBoardEvaluator evaluator, ProgressBar progressBar, bool allowCaching, bool allowAlphaBetaCutting) => 
            () => new MinimaxBot(depth, evaluator, progressBar, allowCaching, allowAlphaBetaCutting);
    }
}
