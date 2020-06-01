using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts
{
    [Serializable]
    public struct SimulationOutput
    {
        public int player0WinsWhite;
        public int player0WinsBlack;

        public int ties;
        
        public int player1WinsWhite;
        public int player1WinsBlack;

        public int player0LosesWhite => player1WinsBlack;
        public int player0LosesBlack => player1WinsWhite;
        public int player1LosesWhite => player0WinsBlack;
        public int player1LosesBlack => player0WinsWhite;

        public int total;

        public List<List<BoardState>> histories;
    }
}
