using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Pieces
{
   public enum PieceRank : byte
   {
      None = 0b_000,

      Man = 0b_100,
      King = 0b_110,
   }
}