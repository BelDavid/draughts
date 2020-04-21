using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draughts.Pieces;
using Draughts.Rules;

namespace Draughts.Players
{
   public abstract class Player
   {

      public Player() { }

      public PieceColor Color { get; set; }


      /// <summary>
      /// Returns null if no move avaiable (player loses)
      /// </summary>
      /// <returns></returns>
      public abstract Move MakeMove(BoardState boardState);

   }
}