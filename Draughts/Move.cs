using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts
{
   public class Move
   {
      public Position[] path;
      public Position[] positionsOfTakenPieces;

      public int promotion = -1; // if != -1   -> tells at which point in move (index of 'path') promotion happens
   }
}
