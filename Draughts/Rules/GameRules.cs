using Draughts.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Rules
{
   public abstract class GameRules
   {
      public readonly byte numberOfRows, numberOfColumns;

      public GameRules(byte numberOfRows, byte numberOfColumns)
      {
         if (numberOfColumns * numberOfRows > 128)
         {
            throw new ArgumentException("Number of places on the board can not exceed 128");
         }
         if (numberOfRows % 2 != 0 || numberOfColumns % 2 != 0)
         {
            throw new ArgumentException("Number of rows and columns must be even");
         }

         this.numberOfRows = numberOfRows;
         this.numberOfColumns = numberOfColumns;
      }

      public abstract List<Move> GetAvaiableMoves(BoardState state, PieceColor onMove);
      public abstract BoardState GetInitialBoardState();
      public abstract Pieces.PieceColor GetStartingColor();
   }
}
