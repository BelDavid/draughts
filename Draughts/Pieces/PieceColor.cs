using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Pieces
{
   public enum PieceColor : byte
   {
      None = 0b_000,

      White = 0b_100,
      Black = 0b_101,
   }
}