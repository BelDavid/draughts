using Draughts.Players;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Game
{
    [Serializable]
    public class GameReplay
    {
        public readonly RulesType rules;
        public readonly List<Move> MoveHistory = new List<Move>();

        public GameReplay(RulesType rules, List<Move> moveHistory)
        {
            this.rules = rules;
            MoveHistory = moveHistory;
        }

        public ReplayBot GetFirstReplayBot()  => new ReplayBot("replayBot0", this, true);
        public ReplayBot GetSecondReplayBot() => new ReplayBot("replayBot1", this, false);
    }
}
