using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Players
{
    public enum BotDifficulty
    {
        Randomized,
        Easy,
        Medium,
        Hard,

#if DEBUG
        Depth10,
#endif
    }
}
