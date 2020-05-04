using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draughts.Game;

namespace Draughts.Players
{
    public class ReplayBot : Player
    {
        private readonly GameReplay gameReplay;
        private int i = 0;

        public ReplayBot(string id, GameReplay gameReplay, bool first) : base(id)
        {
            this.gameReplay = gameReplay;
            if (!first)
            {
                i += 1;
            }
        }

        public override Move MakeMove(BoardState boardState)
        {
            var move = gameReplay.MoveHistory.Count > i ? gameReplay.MoveHistory[i] : null;
            i += 2;
            return move;
        }
    }
}
