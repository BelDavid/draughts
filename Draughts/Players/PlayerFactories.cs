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
        public static PlayerFactory UserFactory(string id) => () => new User(id);
        public static PlayerFactory RandomizedBotFactory(string id) => () => new RandomizedBot(id);
        public static PlayerFactory MinimaxBotFactory(string id, int depth, IBoardEvaluator evaluator, ProgressBar progressBar, bool allowCaching, bool allowAlphaBetaCutting) => 
            () => new MinimaxBot(id, depth, evaluator, progressBar, allowCaching, allowAlphaBetaCutting);
    }
}
