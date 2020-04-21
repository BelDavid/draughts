using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Pieces
{
   public enum PieceType : byte
   {
      None = 0b_000,

      WhiteMan = PieceRank.Man | PieceColor.White,
      BlackMan = PieceRank.Man | PieceColor.Black,

      WhiteKing = PieceRank.King | PieceColor.White,
      BlackKing = PieceRank.King | PieceColor.Black,
   }
}